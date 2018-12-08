using System.Data.SqlClient;
using System.Linq;

namespace System.Collections.Generic
{
    internal static class IDictionaryExtensions
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return collection.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static SqlParameter ToSqlParameter(KeyValuePair<string, object> keyValuePair)
        {
            return new SqlParameter(keyValuePair.Key, keyValuePair.Value);
        }
    }
}
