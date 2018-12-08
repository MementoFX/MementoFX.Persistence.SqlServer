using MementoFX.Persistence.SqlServer.Data;
using System;
using System.Data;

namespace MementoFX.Persistence.SqlServer.Helpers
{
    internal static class TypeHelper
    {
        public static bool IsVariableBinaryOrCharType(SqlDbType sqlDbType)
        {
            return sqlDbType == SqlDbType.VarBinary || sqlDbType == SqlDbType.NVarChar || sqlDbType == SqlDbType.VarChar;
        }

        public static Type GetClrType(string typeName, bool isNullable)
        {
            var sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), typeName, ignoreCase: true);

            switch (sqlDbType)
            {
                case SqlDbType.UniqueIdentifier:
                    return isNullable ? typeof(Guid?) : typeof(Guid);
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    return isNullable ? typeof(DateTime?) : typeof(DateTime);
                case SqlDbType.DateTimeOffset:
                    return isNullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
                case SqlDbType.Time:
                    return isNullable ? typeof(TimeSpan?) : typeof(TimeSpan);
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                case SqlDbType.Xml:
                    return typeof(string);
                case SqlDbType.BigInt:
                    return isNullable ? typeof(long?) : typeof(long);
                case SqlDbType.Int:
                    return isNullable ? typeof(int?) : typeof(int);
                case SqlDbType.SmallInt:
                    return isNullable ? typeof(short?) : typeof(short);
                case SqlDbType.TinyInt:
                    return isNullable ? typeof(byte?) : typeof(byte);
                case SqlDbType.Float:
                    return isNullable ? typeof(double?) : typeof(double);
                case SqlDbType.Real:
                    return isNullable ? typeof(float?) : typeof(float);
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                    return isNullable ? typeof(decimal?) : typeof(decimal);
                case SqlDbType.Bit:
                    return isNullable ? typeof(bool?) : typeof(bool);
                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.Timestamp:
                case SqlDbType.VarBinary:
                    return typeof(byte[]);
                case SqlDbType.Udt:
                case SqlDbType.Variant:
                    return typeof(object);
                default:
                    throw new NotSupportedException($"Unsupported column type: {sqlDbType}");
            }
        }

        private static SqlDbType GetSqlDbType(Type type, bool useCompression)
        {
            if (IsEnumType(type))
            {
                return SqlDbType.Int;
            }

            if (type == typeof(Guid))
            {
                return SqlDbType.UniqueIdentifier;
            }

            if (type == typeof(DateTime))
            {
                return SqlDbType.DateTime2;
            }

            if (type == typeof(DateTimeOffset))
            {
                return SqlDbType.DateTimeOffset;
            }

            if (type == typeof(TimeSpan))
            {
                return SqlDbType.Time;
            }

            if (type == typeof(char) || type == typeof(string))
            {
                return SqlDbType.NVarChar;
            }

            if (type == typeof(long) || type == typeof(ulong))
            {
                return SqlDbType.BigInt;
            }

            if (type == typeof(int) || type == typeof(uint))
            {
                return SqlDbType.Int;
            }

            if (type == typeof(short) || type == typeof(ushort))
            {
                return SqlDbType.SmallInt;
            }

            if (type == typeof(sbyte) || type == typeof(byte))
            {
                return SqlDbType.TinyInt;
            }

            if (type == typeof(double))
            {
                return SqlDbType.Float;
            }

            if (type == typeof(float))
            {
                return SqlDbType.Real;
            }

            if (type == typeof(decimal))
            {
                return SqlDbType.Decimal;
            }

            if (type == typeof(byte[]))
            {
                return SqlDbType.Binary;
            }

            if (type == typeof(bool) || type == typeof(bool?))
            {
                return SqlDbType.Bit;
            }

            return useCompression ? SqlDbType.VarBinary : SqlDbType.NVarChar;
        }

        public static bool IsEnumType(Type type)
        {
            if (IsNullableType(type))
            {
                return Nullable.GetUnderlyingType(type)?.IsEnum ?? false;
            }

            return type.IsEnum;
        }

        public static bool IsBinaryType(SqlDbType sqlDbType)
        {
            return sqlDbType == SqlDbType.Binary || sqlDbType == SqlDbType.Image || sqlDbType == SqlDbType.Timestamp || sqlDbType == SqlDbType.VarBinary;
        }

        public static bool IsNullableType(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        private static Type ToNullableType(this Type type)
        {
            if (type == null || type == typeof(void))
            {
                return null;
            }

            if (!type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                return type;
            }

            return typeof(Nullable<>).MakeGenericType(type);
        }
        
        public static TypeInfo GetTypeInfo(bool isNull, Type type, bool useCompression)
        {
            var isNullable = isNull ? true : IsNullableType(type);

            var isClass = !type.IsValueType && type.IsClass && type != typeof(string) && type != typeof(Enum);

            var sqlDbType = GetSqlDbType(isNullable && type.IsValueType ? Nullable.GetUnderlyingType(type) : type, useCompression);

            return new TypeInfo(sqlDbType, isNullable, isClass);
        }
    }
}
