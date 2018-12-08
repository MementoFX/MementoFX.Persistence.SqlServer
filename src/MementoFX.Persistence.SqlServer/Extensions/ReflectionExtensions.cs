using MementoFX.Persistence.SqlServer.Helpers;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace System.Reflection
{
    internal static class ReflectionExtensions
    {
        public static object ParseFromDataReader(this object value, Type type, bool useCompression)
        {
            if (value == null || value is DBNull)
            {
                if (TypeHelper.IsNullableType(type)) return null;

                return FormatterServices.GetUninitializedObject(type);
            }

            if (value is string @string)
            {
                if (JsonHelper.TryDeserializeObject(@string, type, out object jsonObj))
                {
                    return jsonObj;
                }
            }

            if (value is byte[] bytes && useCompression)
            {
                var decompressedBytes = bytes.GZipDecompress();

                var utf8String = Encoding.UTF8.GetString(decompressedBytes);
                if (JsonHelper.TryDeserializeObject(utf8String, type, out object jsonObj))
                {
                    return jsonObj;
                }
            }

            return value;
        }

        public static object ConvertFromDataReader(this object value, Type type, string propertyName, bool useCompression)
        {
            var propertyType = type.GetPublicOrPrivateProperty(propertyName)?.PropertyType;
            if (propertyType == null)
            {
                throw new ArgumentNullException(nameof(propertyType));
            }

            if (value == null || value is DBNull)
            {
                if (TypeHelper.IsNullableType(propertyType)) return null;

                return FormatterServices.GetUninitializedObject(propertyType);
            }

            if (value is string @string)
            {
                if (JsonHelper.TryDeserializeObject(@string, propertyType, out object jsonObj))
                {
                    return jsonObj;
                }
            }

            if (value is byte[] bytes && useCompression)
            {
                var decompressedBytes = bytes.GZipDecompress();

                var utf8String = Encoding.UTF8.GetString(decompressedBytes);
                if (JsonHelper.TryDeserializeObject(utf8String, propertyType, out object jsonObj))
                {
                    return jsonObj;
                }
            }

            if (value is int @int && TypeHelper.IsEnumType(propertyType))
            {
                var enumType = TypeHelper.IsNullableType(propertyType) ? Nullable.GetUnderlyingType(propertyType) : propertyType;

                var enumValues = Enum.GetValues(enumType);

                var values = new object[enumValues.Length];

                enumValues.CopyTo(values, 0);

                return values.SingleOrDefault(v => (int)v == @int) ?? value;
            }

            return value;
        }

        public static PropertyInfo GetPublicOrPrivateProperty(this Type type, string propertyName)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static void SetPropertyValue(this object obj, string propertyName, object val)
        {
            SetPropertyValue(obj, obj.GetType(), propertyName, val);
        }

        public static void SetPropertyValue(this object obj, Type type, string propertyName, object val)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            
            if (type.BaseType != typeof(object))
            {
                SetPropertyValue(obj, type.BaseType, propertyName, val);
                return;
            }
            
            var propInfo = type.GetPublicOrPrivateProperty(propertyName);
            if (propInfo == null)
            {
                throw new ArgumentOutOfRangeException(nameof(propertyName), string.Format("Property {0} not found in type {1}", propertyName, type.FullName));
            }

            propInfo.SetValue(obj, val, null);
        }
    }
}
