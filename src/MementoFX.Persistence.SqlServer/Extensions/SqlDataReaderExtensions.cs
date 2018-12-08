using MementoFX.Persistence.SqlServer;
using MementoFX.Persistence.SqlServer.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace System.Data.Common
{
    internal static class SqlDataReaderExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this SqlDataReader dataReader, bool useCompression, bool useSingleTable)
        {
            return dataReader.AsEnumerable(typeof(T), useCompression, useSingleTable).Cast<T>();
        }

        public static IEnumerable<object> AsEnumerable(this SqlDataReader dataReader, Type type, bool useCompression, bool useSingleTable)
        {
            if (dataReader == null)
            {
                throw new ArgumentNullException(nameof(dataReader));
            }

            object instance;

            var list = new List<object>();

            if (useSingleTable)
            {
                while (dataReader.Read())
                {
                    var dictionary = new Dictionary<string, object>();

                    foreach (DataRow drow in dataReader.GetSchemaTable().Rows)
                    {
                        var keyName = drow.ItemArray[0].ToString();

                        dictionary.Add(keyName, dataReader[keyName]);
                    }

                    var typeFullNameObj = dictionary[SqlServerEventStore.TypeSingleTableColumnName];
                    if (typeFullNameObj == null)
                    {
                        continue;
                    }

                    var typeFullName = typeFullNameObj as string;
                    if (string.IsNullOrWhiteSpace(typeFullName))
                    {
                        continue;
                    }

                    var eventType = Type.GetType(typeFullName);
                    if (eventType == null)
                    {
                        continue;
                    }

                    var eventData = dictionary[SqlServerEventStore.EventSingleTableColumnName];
                    if (eventData == null)
                    {
                        continue;
                    }

                    instance = eventData.ParseFromDataReader(eventType, useCompression);
                    
                    list.Add(instance);
                }
            }
            else
            {
                var constructorInfo = type.GetConstructors().FirstOrDefault(p => p.IsPublic);
                if (constructorInfo == null)
                {
                    throw new ArgumentNullException(nameof(constructorInfo));
                }

                var objectActivator = ReflectionHelper.GetActivator(constructorInfo);

                while (dataReader.Read())
                {
                    var dictionary = new Dictionary<string, object>();

                    foreach (DataRow drow in dataReader.GetSchemaTable().Rows)
                    {
                        var keyName = drow.ItemArray[0].ToString();

                        dictionary.Add(keyName, dataReader[keyName]);
                    }

                    var parameters = constructorInfo.GetParameters();

                    var values = new object[parameters.Length];

                    for (var p = 0; p < parameters.Length; p++)
                    {
                        var kvp = dictionary.SingleOrDefault(pv => string.Equals(pv.Key, parameters[p].Name, StringComparison.OrdinalIgnoreCase));
                        if (kvp.Value == null || kvp.Equals(default(KeyValuePair<string, object>))) continue;

                        values[p] = kvp.Value.ConvertFromDataReader(type, kvp.Key, useCompression);

                        dictionary.Remove(kvp.Key);
                    }

                    instance = objectActivator(values);

                    if (dictionary.Keys.Count > 0)
                    {
                        foreach (var kvp in dictionary)
                        {
                            var value = kvp.Value.ConvertFromDataReader(type, kvp.Key, useCompression);

                            instance.SetPropertyValue(kvp.Key, value);
                        }
                    }

                    list.Add(instance);
                }
            }

            return list;
        }
    }
}
