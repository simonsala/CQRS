using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;
using CQRS.Events;
using CQRS.Exceptions;
using Microsoft.Data.Sqlite;

namespace CQRS.EventSources
{
    public class SqliteEventSource : ISqlEventSource
    {
        private readonly string _connectionString;

        public SqliteEventSource(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public bool CommitEvent<E>(string aggregateQualifiedName, int currentVersion, E @event) where E : Event
        {
            using SqliteConnection connection = new SqliteConnection(this._connectionString);

            try
            {
                connection.Open();

                using var transaction = connection.BeginTransaction();

                try
                {


                    using var command = connection.CreateCommand();

                    command.Connection = connection;
                    command.Transaction = transaction;

                    var creationTime = DateTimeOffset.Now;
                    var eventData = JsonSerializer.Serialize(@event);
                    var eventType = typeof(E).AssemblyQualifiedName;
                    var version = GetVersion(@event.AggregateId);

                    if (version > currentVersion)
                        throw new ConcurrencyException($"A concurrency issue has occurred.");

                    command.CommandText = "select Count(1) FROM Aggregates WHERE AggregateId = @AggregateId";
                    command.Parameters.Add(new SqliteParameter("@AggregateId", SqliteType.Text));
                    command.Parameters["@AggregateId"].Value = @event.AggregateId.ToString();

                    var count = 0;

                    using (var sqlReader = command.ExecuteReader())
                    {
                        if (sqlReader.Read())
                            count = sqlReader.GetInt32(0);
                    }

                    if (count == 1)
                    {
                        command.CommandText = "select Version FROM Aggregates WHERE AggregateId = @AggregateId";
                        version = 0;
                        using (var sqlReader = command.ExecuteReader())
                        {
                            if (sqlReader.Read())
                                version = sqlReader.GetInt32(0) + 1;
                        }

                        command.Parameters.Add(new SqliteParameter("@Version", SqliteType.Integer));
                        command.Parameters.Add(new SqliteParameter("@CreationTime", SqliteType.Text));
                        command.Parameters["@Version"].Value = version;
                        command.Parameters["@CreationTime"].Value = creationTime.ToString();
                        command.CommandText =
                            "update Aggregates set Version = @Version, LastModified = @CreationTime where AggregateId = @AggregateId";
                        command.ExecuteNonQuery();

                        command.Parameters.Add(new SqliteParameter("@EventId", SqliteType.Text));
                        command.Parameters.Add(new SqliteParameter("@EventType", SqliteType.Text));
                        command.Parameters.Add(new SqliteParameter("@EventData", SqliteType.Text));
                        command.Parameters["@EventId"].Value = @event.EventId.ToString();
                        command.Parameters["@EventType"].Value = eventType;
                        command.Parameters["@EventData"].Value = eventData;
                        command.CommandText =
                            "insert into Events (EventId, EventType, Version, EventData, AggregateId, CreationTime) VALUES (@EventId, @EventType, @Version, @EventData, @AggregateId, @CreationTime)";
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        command.Parameters.Add(new SqliteParameter("@AggregateType", SqliteType.Text));
                        command.Parameters.Add(new SqliteParameter("@CreationTime", SqliteType.Text));
                        command.Parameters["@AggregateType"].Value = aggregateQualifiedName;
                        command.Parameters["@CreationTime"].Value = creationTime.ToString();
                        command.CommandText =
                            "insert into Aggregates (AggregateId, Version, AggregateType, LastModified) VALUES (@AggregateId, 1, @AggregateType, @CreationTime)";
                        command.ExecuteNonQuery();

                        command.CommandText =
                            "insert into Events (EventId, EventType, Version, EventData, AggregateId, CreationTime) VALUES (@EventId, @EventType, 1, @EventData, @AggregateId, @CreationTime)";
                        command.Parameters.Add(new SqliteParameter("@EventId", SqliteType.Text));
                        command.Parameters.Add(new SqliteParameter("@EventType", SqliteType.Text));
                        command.Parameters.Add(new SqliteParameter("@EventData", SqliteType.Text));
                        command.Parameters["@EventId"].Value = @event.EventId.ToString();
                        command.Parameters["@EventType"].Value = eventType;
                        command.Parameters["@EventData"].Value = eventData;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    Console.WriteLine("transaction successful!");
                    return true;
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                        throw new SqlEventSourceException($"CommitEvent method from SqliteEventSource. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Exception: {ex.Message}.");
                    }
                    catch (Exception ex2)
                    {
                        throw new SqlEventSourceException($"CommitEvent method from SqliteEventSource. Transaction rollback failed. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Inner Exception: {ex.Message}. Outer Exception: {ex2.Message}");
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                throw new SqlEventSourceException($"CommitEvent method from SqliteEventSource. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Exception: {ex.Message}.");
            }
            finally
            {
                connection.Close();
            }

            return false;
        }

        public List<EventSource> GetEventSources(Guid aggregateId)
        {
            var eventSources = new List<EventSource>();

            using SqliteConnection connection = new SqliteConnection(this._connectionString);

            try
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "select * from Events where AggregateId = @AggregateId order by Version asc";
                command.Parameters.Add(new SqliteParameter("@AggregateId", SqliteType.Text));
                command.Parameters["@AggregateId"].Value = aggregateId.ToString();

                using var sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    eventSources.Add(new EventSource()
                    {
                        EventId = Guid.Parse(sqlReader.GetString(0)),
                        EventType = sqlReader.GetString(1),
                        Version = sqlReader.GetInt32(2),
                        EventData = sqlReader.GetString(3),
                        AggregateId = Guid.Parse(sqlReader.GetString(4)),
                        CreationTime = DateTimeOffset.Parse(sqlReader.GetString(5))
                    });
                }
            }
            catch (Exception ex)
            {
                throw new SqlEventSourceException($"GetEventSources method from SqliteEventSource. AggregateId: {aggregateId}. Exception: {ex.Message}.");
            }  
            finally
            {
                connection.Close();
            }

            return eventSources;
        }

        public void ScaffoldEventSourcing()
        {
            if (TablesExists()) return;
            CreateAggregatesTable();
            CreateEventsTable();
        }

        public bool TablesExists()
        {
            using SqliteConnection connection = new SqliteConnection(this._connectionString);
            
            try
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(name) FROM sqlite_master WHERE type = 'table' and name = 'Aggregates' or name = 'Events'";

                var sqlReader = command.ExecuteReader();

                var count = "";

                if (sqlReader.Read())
                {
                    count = sqlReader.GetString(0);
                }

                var tablesExists = (count == "2");
                return tablesExists;
            }
            finally
            {
                connection.Close();
            }
        }

        public void CreateEventsTable()
        {
            using SqliteConnection connection = new SqliteConnection(this._connectionString);

            try
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText =
                @"create table Events(EventId text primary key NOT NULL, EventType text NULL, Version integer NOT NULL, 
                EventData text NOT NULL, AggregateId text NOT NULL, CreationTime text NOT NULL)";

                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        public void CreateAggregatesTable()
        {
            using SqliteConnection connection = new SqliteConnection(this._connectionString);

            try
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText =
                @"create table Aggregates(AggregateId text primary key NOT NULL,
                  AggregateType text NULL, Version integer NOT NULL, LastModified text NOT NULL)";

                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        public int GetVersion(Guid aggregateId)
        {
            var version = 0;
            
            using SqliteConnection connection = new SqliteConnection(this._connectionString);

            try
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "select Version FROM Aggregates WHERE AggregateId = @AggregateId";
                command.Parameters.Add(new SqliteParameter("@AggregateId", SqliteType.Text));
                command.Parameters["@AggregateId"].Value = aggregateId.ToString();

                using var sqlReader = command.ExecuteReader();

                if (sqlReader.Read())
                {
                    version = sqlReader.GetInt32(0);
                }
            }
            finally
            {
                connection.Close();
            }

            return version;
        }

        public void RemoveEventSourcing()
        {
            if (!TablesExists())
                return;

            using SqliteConnection connection = new SqliteConnection(this._connectionString);

            try
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText =
                @"drop table Events";
                command.ExecuteNonQuery();

                command.CommandText =
                @"drop table Aggregates";
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
