using MementoFX.Messaging;
using MementoFX.Persistence.SqlServer.Data;
using MementoFX.Persistence.SqlServer.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;

namespace MementoFX.Persistence.SqlServer
{
    /// <summary>
    /// Provides an implementation of a Memento event store
    /// using Sql Server as the storage
    /// </summary>
    public class SqlServerEventStore : EventStore
    {
        private readonly Configuration.Settings Settings;

        internal const string EventSingleTableColumnName = "Event";
        internal const string TypeSingleTableColumnName = "Type";

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="connectionString">The connection string of document store to be used by the instance</param>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public SqlServerEventStore(string connectionString, IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            this.Settings = new Configuration.Settings(connectionString);
            
            DatabaseHelper.CreateDatabaseIfNotExists(this.Settings.ConnectionString);
        }

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="settings">The document store settings to be used by the instance</param>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public SqlServerEventStore(Configuration.Settings settings, IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            
            DatabaseHelper.CreateDatabaseIfNotExists(this.Settings.ConnectionString);
        }

        /// <summary>
        /// Retrieves all events of a type which satisfy a requirement
        /// </summary>
        /// <typeparam name="T">The type of the event</typeparam>
        /// <param name="filter">The requirement</param>
        /// <returns>The events which satisfy the given requirement</returns>
        public override IEnumerable<T> Find<T>(Expression<Func<T, bool>> filter)
        {
            var tableName = this.Settings.UseSingleTable ? "Events" : typeof(T).Name;

            using (var connection = new SqlConnection(this.Settings.ConnectionString))
            {
                var tableExists = connection.CheckIfTableExists(tableName);
                if (!tableExists)
                {
                    return new T[0];
                }
            }
            
            var sqlExpression = filter.ToSqlExpression(this.Settings.UseCompression, this.Settings.UseSingleTable);

            var commandText = Commands.BuildSelectWhereCommandText(tableName, "*") + " " + sqlExpression.CommandText;

            var parameters = sqlExpression.Parameters.Select(IDictionaryExtensions.ToSqlParameter).ToArray();

            using (var connection = new SqlConnection(this.Settings.ConnectionString))
            {
                return connection.Query<T>(commandText, this.Settings.UseCompression, this.Settings.UseSingleTable, parameters);
            }
        }

        /// <summary>
        /// Retrieves the desired events from the store
        /// </summary>
        /// <param name="aggregateId">The aggregate id</param>
        /// <param name="pointInTime">The point in time up to which the events have to be retrieved</param>
        /// <param name="eventDescriptors">The descriptors defining the events to be retrieved</param>
        /// <param name="timelineId">The id of the timeline from which to retrieve the events</param>
        /// <returns>The list of the retrieved events</returns>
        public override IEnumerable<DomainEvent> RetrieveEvents(Guid aggregateId, DateTime pointInTime, IEnumerable<EventMapping> eventDescriptors, Guid? timelineId)
        {
            var events = new List<DomainEvent>();
            
            foreach (var descriptorsGroup in eventDescriptors.GroupBy(e => e.EventType))
            {
                var eventType = descriptorsGroup.Key;
                var tableName = this.Settings.UseSingleTable ? "Events" : eventType.Name;

                using (var connection = new SqlConnection(this.Settings.ConnectionString))
                {
                    var tableExists = connection.CheckIfTableExists(tableName);
                    if (!tableExists)
                    {
                        continue;
                    }
                }

                var commandText = Commands.BuildSelectWhereCommandText(tableName, "*");

                var parameters = new List<SqlParameter>();
                
                var counter = 1;

                var filters = new List<string>();

                for (var i = 0; i < descriptorsGroup.Count(); i++)
                {
                    var eventDescriptor = descriptorsGroup.ElementAt(i);

                    var parameter = new SqlParameter(string.Format(Commands.ParameterNameFormat, counter), aggregateId);
                    counter++;

                    var leftPart = TableHelper.GetFixedLeftPart(eventDescriptor.AggregateIdPropertyName, this.Settings.UseCompression, this.Settings.UseSingleTable);

                    var filter = Commands.JoinWithSpace(leftPart, "=", parameter.ParameterName);

                    filters.Add(filter);
                    
                    parameters.Add(parameter);
                }

                var filtersText = filters.Count == 1 ? filters[0] : Commands.Enclose(string.Join(" OR ", filters));

                commandText = Commands.JoinWithSpace(commandText, filtersText);

                if (this.Settings.UseSingleTable)
                {
                    var typeColumnParameter = new SqlParameter(string.Format(Commands.ParameterNameFormat, counter), eventType.GetFullTypeAndAssemblyName());
                    counter++;

                    commandText = Commands.JoinWithSpace(commandText, "AND", TypeSingleTableColumnName, "=", typeColumnParameter.ParameterName);

                    parameters.Add(typeColumnParameter);
                }

                var timeStampParameter = new SqlParameter(string.Format(Commands.ParameterNameFormat, counter), pointInTime);
                counter++;

                commandText = Commands.JoinWithSpace(commandText, "AND", nameof(DomainEvent.TimeStamp), "<=", timeStampParameter.ParameterName);
                
                parameters.Add(timeStampParameter);

                if (!timelineId.HasValue)
                {
                    commandText = Commands.JoinWithSpace(commandText, "AND", nameof(DomainEvent.TimelineId), "IS", "NULL");
                }
                else
                {
                    var timelineIdParameter = new SqlParameter(string.Format(Commands.ParameterNameFormat, counter), timelineId.Value);
                    counter++;

                    commandText += Commands.JoinWithSpace(commandText, "AND", Commands.Enclose(nameof(DomainEvent.TimelineId), "IS", "NULL", "OR", nameof(DomainEvent.TimelineId), "=", timelineIdParameter.ParameterName));

                    parameters.Add(timelineIdParameter);
                }

                IEnumerable<object> collection = null;

                using (var connection = new SqlConnection(this.Settings.ConnectionString))
                {
                    collection = connection.Query(descriptorsGroup.Key, commandText, this.Settings.UseCompression, this.Settings.UseSingleTable, parameters);
                }

                if (collection != null)
                {
                    foreach (var evt in collection)
                    {
                        events.Add((DomainEvent)evt);
                    }
                }
            }

            return events.OrderBy(e => e.TimeStamp);
        }

        /// <summary>
        /// Saves an event into the store
        /// </summary>
        /// <param name="event">The event to be saved</param>
        protected override void _Save(DomainEvent @event)
        {
            var eventType = @event.GetType();
            var tableName = this.Settings.UseSingleTable ? "Events" : eventType.Name;

            using (var connection = new SqlConnection(this.Settings.ConnectionString))
            {
                connection.CreateOrUpdateTable(@event, eventType, tableName, this.Settings.AutoIncrementalTableMigrations, this.Settings.UseCompression, this.Settings.UseSingleTable);
            }
            
            var parametersData = @event.GetParametersData(eventType, this.Settings.UseCompression, this.Settings.UseSingleTable);

            var parameters = parametersData.Select(ParameterDataExtensions.ToSqlParameter);
            
            var commandText = Commands.BuildInsertCommandText(tableName, parametersData.Select(t => t.Name).ToArray(), parameters.Select(parameter => parameter.ParameterName).ToArray());
            
            using (var connection = new SqlConnection(this.Settings.ConnectionString))
            {
                connection.ExecuteNonQuery(commandText, parameters);
            }
        }
    }
}
