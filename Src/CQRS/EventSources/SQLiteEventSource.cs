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
    public class SQLiteEventSource : ISqlEventSource
    {
        private readonly string _connectionString;
        private bool _memoryMode;
        private SqliteConnection _connection;

        public SQLiteEventSource()
        {
            this._connectionString = @"Data Source=InMemorySample;Mode=Memory;Cache=Shared";
            this._memoryMode = true;
            this._connection = new SqliteConnection(_connectionString);
            _connection.Open();

        }

        public SQLiteEventSource(string connectionString)
        {
            this._connectionString = connectionString;
            this._memoryMode = false;
        }

        public bool CommitEvent<E>(string aggregateQualifiedName, int currentVersion, E @event) where E : Event
        {
            OpenConnection(); 
            var command = _connection.CreateCommand();
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    command.Connection = _connection;
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
                        //Update Aggregate
                        command.CommandText =
                            "update Aggregates set Version = @Version, LastModified = @CreationTime where AggregateId = @AggregateId";
                        command.ExecuteNonQuery();

                        //Insert Event
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
                        throw new SqlEventSourceException($"Transaction rollback failed. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Inner Exception: {ex.Message}.");
                    }
                    catch (Exception ex2)
                    {
                        throw new SqlEventSourceException($"Transaction rollback failed. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Inner Exception: {ex.Message}. Outer Exception: {ex2.Message}");
                    }
                }
                finally
                {
                    CloseConnection();
                }
            }
            return false;
        }

        public List<EventSource> GetEventSources(Guid aggregateId)
        {
            var eventSources = new List<EventSource>();
           
            try
            {
                OpenConnection();
                _connection.Open();

                var command = _connection.CreateCommand();
                command.CommandText = "select * from Events where AggregateId = @AggregateId order by Version asc";
                command.Parameters.Add(new SqliteParameter("@AggregateId", SqliteType.Text));
                command.Parameters["@AggregateId"].Value = aggregateId.ToString();
                using (var sqlReader = command.ExecuteReader())
                {
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
            }
            finally
            {
                CloseConnection();
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
            try
            {
                OpenConnection();
        
                var command = _connection.CreateCommand();
                command.Connection = _connection;
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
                CloseConnection();
            }
        }

        public void CreateEventsTable()
        {

            try
            {
                OpenConnection();

                var command = _connection.CreateCommand();
                command.CommandText =
                @"create table Events(EventId text primary key NOT NULL, EventType text NULL, Version integer NOT NULL, 
                EventData text NOT NULL, AggregateId text NOT NULL, CreationTime text NOT NULL)";

                command.ExecuteNonQuery();
            }
            finally
            {
                CloseConnection();
            }
        }

        public void CreateAggregatesTable()
        {
            try
            {
                OpenConnection();

                var command = _connection.CreateCommand();
                command.CommandText =
                @"create table Aggregates(AggregateId text primary key NOT NULL,
                  AggregateType text NULL, Version integer NOT NULL, LastModified text NOT NULL)";

                command.ExecuteNonQuery();
            }
            finally
            {
                CloseConnection();
            }
        }

        public int GetVersion(Guid aggregateId)
        {
            var version = 0;

            try
            {
                OpenConnection();

                var command = _connection.CreateCommand();
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
                CloseConnection();
            }

            return version;
        }

        private void OpenConnection()
        {
            if (!_memoryMode)
            {
                _connection = new SqliteConnection(_connectionString);
                _connection.Open();
            }
        }

        private void CloseConnection()
        {
            if (!_memoryMode)
            {
                _connection.Close();
            }
        }
    }
}
