using MementoFX.Persistence.SqlServer.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace MementoFX.Persistence.SqlServer.Data
{
    internal static class ParameterDataExtensions
    {
        public static IEnumerable<ParameterData> GetParametersData(this object obj, Type type, bool useCompression, bool useSingleTable)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var properties = (useSingleTable ? typeof(DomainEvent) : type).GetProperties();
            if (properties.Length == 0)
            {
                throw new InvalidOperationException();
            }

            foreach (var pi in properties)
            {
                var value = pi.GetValue(obj);

                var typeInfo = TypeHelper.GetTypeInfo(value == null, pi.PropertyType, useCompression);

                value = FixValue(value, typeInfo.IsClass, useCompression);

                yield return new ParameterData(pi.Name, value, typeInfo.Type);
            }

            if (useSingleTable)
            {
                var typeColumnValue = FixValue(type.GetFullTypeAndAssemblyName(), isClass: false, useCompression: useCompression);

                var typeColumnTypeInfo = TypeHelper.GetTypeInfo(false, typeof(string), useCompression);

                yield return new ParameterData(SqlServerEventStore.TypeSingleTableColumnName, typeColumnValue, typeColumnTypeInfo.Type);

                var eventColumnValue = FixValue(obj, isClass: true, useCompression: useCompression);

                var eventColumnTypeInfo = TypeHelper.GetTypeInfo(false, typeof(string), useCompression);
                
                yield return new ParameterData(SqlServerEventStore.EventSingleTableColumnName, eventColumnValue, eventColumnTypeInfo.Type);
            }
        }
        
        private static object FixValue(object value, bool isClass, bool useCompression)
        {
            if (value == null)
            {
                return DBNull.Value;
            }
            
            if (isClass)
            {
                var @string = JsonConvert.SerializeObject(value);
                if (useCompression)
                {
                    var utf8Bytes = Encoding.UTF8.GetBytes(@string);

                    var bytes = utf8Bytes.GZipCompress();

                    return bytes;
                }

                return @string;
            }

            if (TypeHelper.IsEnumType(value.GetType()))
            {
                return Convert.ToString((int)value, CultureInfo.InvariantCulture);
            }

            return value;
        }

        public static SqlParameter ToSqlParameter(ParameterData parameterData, int index)
        {
            var parameterName = string.Format(Commands.ParameterNameFormat, index + 1);
            
            if (TypeHelper.IsBinaryType(parameterData.Type) && !parameterData.HasValue)
            {
                return new SqlParameter(parameterName, parameterData.Type, -1)
                {
                    Value = DBNull.Value
                };
            }

            return new SqlParameter(parameterName, parameterData.Value);
        }
    }
}
