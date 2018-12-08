using MementoFX.Persistence.SqlServer.Data;
using System.Data.SqlClient;

namespace MementoFX.Persistence.SqlServer.Helpers
{
    internal static class DatabaseHelper
    {
        public static void CreateDatabaseIfNotExists(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

            var databaseName = connectionStringBuilder.InitialCatalog;

            connectionStringBuilder.InitialCatalog = "master";

            using (var connection = new SqlConnection(connectionStringBuilder.ToString()))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = Commands.SelectDatabaseNamesWhereFormat;

                    command.Parameters.AddWithValue(Commands.DatabaseNameParameterName, databaseName);

                    command.Connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            connection.Close();
                            return;
                        }
                    }
                    
                    command.CommandText = string.Format(Commands.CreateDatabaseFormat, databaseName);

                    command.ExecuteNonQuery();

                    connection.Close();
                }
            }
        }
    }
}
