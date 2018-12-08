using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace System.Data.SqlClient
{
    public static class SqlConnectionExtensions
    {
        public static void ExecuteNonQuery(this SqlConnection connection, string commandText, IEnumerable<SqlParameter> parameters = null)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException(nameof(commandText));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.Parameters.AddRange(parameters);

                command.Connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
            }
        }

        public static IEnumerable<T> Query<T>(this SqlConnection connection, string commandText, bool useCompression, bool useSingleTable, IEnumerable<SqlParameter> parameters = null)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException(nameof(commandText));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.Parameters.AddRange(parameters);

                command.Connection.Open();

                var dataReader = command.ExecuteReader();

                var data = dataReader.AsEnumerable<T>(useCompression, useSingleTable);

                dataReader.Close();

                connection.Close();

                return data;
            }
        }

        public static IEnumerable<object> Query(this SqlConnection connection, Type type, string commandText, bool useCompression, bool useSingleTable, IEnumerable<SqlParameter> parameters = null)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException(nameof(commandText));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.Parameters.AddRange(parameters);

                command.Connection.Open();

                var dataReader = command.ExecuteReader();

                var data = dataReader.AsEnumerable(type, useCompression, useSingleTable);

                dataReader.Close();

                connection.Close();

                return data;
            }
        }
    }
}
