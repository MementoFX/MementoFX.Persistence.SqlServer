using MementoFX.Persistence.SqlServer.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace MementoFX.Persistence.SqlServer.Helpers
{
    internal static class TableHelper
    {
        private static void AlterTableAddColumn(this SqlConnection connection, PropertyInfo property, object obj, string tableName, bool useCompression)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            using (var command = connection.CreateCommand())
            {
                var typeInfo = TypeHelper.GetTypeInfo(property.GetValue(obj) == null, property.PropertyType, useCompression);

                command.CommandText = string.Format(Commands.AlterTableAddColumnFormat, tableName, property.Name, typeInfo.ToString());

                command.Connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
            }
        }

        public static bool CheckIfTableExists(this SqlConnection connection, string tableName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(nameof(tableName));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = Commands.SelectTableExistsFormat;

                command.Parameters.AddWithValue(Commands.TableNameParameterName, tableName);

                command.Connection.Open();

                var result = (int?)command.ExecuteScalar();

                connection.Close();

                return result > 0;
            }
        }

        private static void CreateIndex(this SqlConnection connection, string tableName, string columnName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            using (var command = connection.CreateCommand())
            {
                var indexName = string.Format(Commands.CreateIndexNameFormat, tableName, columnName);

                command.CommandText = string.Format(Commands.CreateIndexFormat, indexName, tableName, columnName);

                command.Connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
            }
        }

        public static void CreateOrUpdateTable(this SqlConnection connection, object obj, Type type, string tableName, bool autoIncrementalTableMigrations, bool useCompression, bool useSingleTable)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(nameof(tableName));
            }

            if (!connection.CheckIfTableExists(tableName))
            {
                connection.CreateTable(obj, type, tableName, useCompression, useSingleTable);

                var indexesColumnNames = GetBaseDomainEventProperties();
                if (indexesColumnNames != null && indexesColumnNames.Length > 0)
                {
                    foreach (var columnName in indexesColumnNames)
                    {
                        connection.CreateIndex(tableName, columnName);
                    }
                }
            }
            else if (!useSingleTable && autoIncrementalTableMigrations)
            {
                var tableSchema = connection.GetTableSchema(tableName);

                var properties = type.GetProperties().Where(property => tableSchema.All(t => !string.Equals(t.Item1, property.Name, StringComparison.OrdinalIgnoreCase))).ToArray();
                if (properties.Length > 0)
                {
                    foreach (var property in properties)
                    {
                        connection.AlterTableAddColumn(property, obj, tableName, useCompression);
                    }
                }
            }
        }

        private static void CreateTable(this SqlConnection connection, object obj, Type type, string tableName, bool useCompression, bool useSingleTable)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var commandText = string.Empty;

            var properties = (useSingleTable ? typeof(DomainEvent) : type).GetProperties();
            if (properties.Length == 0)
            {
                throw new InvalidOperationException();
            }

            for (var i = 0; i < properties.Length; i++)
            {
                var typeInfo = TypeHelper.GetTypeInfo(properties[i].GetValue(obj) == null, properties[i].PropertyType, useCompression);

                var value = typeInfo.ToString();

                if (!typeInfo.IsNullable)
                {
                    value = Commands.JoinWithSpace(value, "NOT NULL");
                }

                commandText += Commands.JoinWithSpace(i == 0 ? string.Empty : ",", properties[i].Name, value);
            }

            if (useSingleTable)
            {
                var typeColumnTypeInfo = TypeHelper.GetTypeInfo(false, typeof(string), useCompression);

                var typeColumnValue = Commands.JoinWithSpace(typeColumnTypeInfo.ToString(), "NOT NULL");

                commandText += Commands.JoinWithSpace(",", SqlServerEventStore.TypeSingleTableColumnName, typeColumnValue);

                var eventColumnTypeInfo = TypeHelper.GetTypeInfo(false, typeof(string), useCompression);

                var eventColumnValue = Commands.JoinWithSpace(eventColumnTypeInfo.ToString(), "NOT NULL");

                commandText += Commands.JoinWithSpace(",", SqlServerEventStore.EventSingleTableColumnName, eventColumnValue);
            }

            commandText = Commands.JoinWithSpace(string.Format(Commands.CreateTableFormat, tableName), Commands.Enclose(commandText.Trim()));

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                command.Connection.Open();

                command.ExecuteNonQuery();

                connection.Close();
            }
        }

        private static string[] GetBaseDomainEventProperties()
        {
            return new[] { nameof(DomainEvent.Id), nameof(DomainEvent.TimelineId), nameof(DomainEvent.TimeStamp) };
        }

        public static string GetFixedLeftPart(string text, bool useCompression, bool useSingleTable)
        {
            if (!text.Contains('.') && !useSingleTable)
            {
                return text;
            }

            string jsonExpression, jsonPath;

            if (useSingleTable)
            {
                jsonExpression = SqlServerEventStore.EventSingleTableColumnName;

                if (useCompression)
                {
                    jsonExpression = string.Format(Commands.ConvertAsVarCharMax, string.Format(Commands.DecompressFormat, jsonExpression));
                }

                jsonPath = "$." + text;
            }
            else
            {
                var splittedAggregateIdPropertyName = text.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                jsonExpression = splittedAggregateIdPropertyName[0];

                if (useCompression)
                {
                    jsonExpression = string.Format(Commands.ConvertAsVarCharMax, string.Format(Commands.DecompressFormat, jsonExpression));
                }

                jsonPath = "$." + string.Join(".", splittedAggregateIdPropertyName.Skip(1));
            }

            return string.Format(Commands.JsonValueFormat, jsonExpression, jsonPath);
        }

        public static IList<Tuple<string, Type>> GetTableSchema(this SqlConnection connection, string tableName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(Commands.SelectTopFormat, 1, "*", tableName);

                command.Connection.Open();

                DataRow[] rows = null;

                using (var dataReader = command.ExecuteReader())
                {
                    using (var schemaTable = dataReader.GetSchemaTable())
                    {
                        rows = schemaTable.Rows.Cast<DataRow>().ToArray();
                    }
                }

                connection.Close();

                return rows?.Select(MapColumnType).ToArray() ?? new Tuple<string, Type>[0];
            }
        }

        private static Tuple<string, Type> MapColumnType(DataRow row)
        {
            var columnName = row[nameof(DbColumn.ColumnName)].ToString();
            var columnDataTypeName = row[nameof(DbColumn.DataTypeName)].ToString();
            var columnAllowDbNull = Convert.ToBoolean(row[nameof(DbColumn.AllowDBNull)]);

            var columnType = TypeHelper.GetClrType(columnDataTypeName, columnAllowDbNull);

            return Tuple.Create(columnName, columnType);
        }
    }
}
