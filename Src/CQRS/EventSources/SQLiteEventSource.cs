using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;
using CQRS.Events;
using Microsoft.Data.Sqlite;

namespace CQRS.EventSources
{
    public class SQLiteEventSource : ISqlEventSource
    {
        private readonly string _connectionString;

        public SQLiteEventSource()
        {
            this._connectionString = @"Data Source=InMemorySample;Mode=Memory;Cache=Shared";
        }

        public SQLiteEventSource(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public bool CommitEvent<E>(string aggregateQualifiedName, E @event) where E : Event
        {
            using (SqliteConnection connection = new SqliteConnection(this._connectionString))
            {

                connection.Open();

                var command = connection.CreateCommand();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        command.Connection = connection;
                        command.Transaction = transaction;

                        var creationTime = DateTimeOffset.Now;
                        var eventData = JsonSerializer.Serialize(@event);
                        var eventType = typeof(E).AssemblyQualifiedName;
                      
                        command.CommandText = "select Count(1) FROM Aggregates WHERE AggregateId = @AggregateId";
                        command.Parameters.Add(new SqlParameter("@AggregateId", SqlDbType.UniqueIdentifier));
                        command.Parameters["@AggregateId"].Value = @event.AggregateId;
                        
                        var count = (int) command.ExecuteScalar();
                        if (count == 1)
                        {
                            command.CommandText = "select Version FROM Aggregates WHERE AggregateId = @AggregateId";
                            var version = 0;
                            using (var sqlReader = command.ExecuteReader())
                            {
                                if (sqlReader.Read())
                                {
                                    version = sqlReader.GetInt32(0) + 1;
                                }
                            }
                            command.Parameters.Add(new SqlParameter("@Version", SqlDbType.Int));
                            command.Parameters.Add(new SqlParameter("@CreationTime", SqlDbType.DateTimeOffset));
                            command.Parameters["@Version"].Value = version;
                            command.Parameters["@CreationTime"].Value = creationTime;
                            //Update Aggregate
                            command.CommandText =
                                "update Aggregates set Version = @Version, LastModified = @CreationTime where AggregateId = @AggregateId";
                            command.ExecuteNonQuery();

                            //Insert Event
                            command.Parameters.Add(new SqlParameter("@EventId", SqlDbType.UniqueIdentifier));
                            command.Parameters.Add(new SqlParameter("@EventType", SqlDbType.VarChar));
                            command.Parameters.Add(new SqlParameter("@EventData", SqlDbType.VarChar));
                            command.Parameters["@EventId"].Value = @event.EventId;
                            command.Parameters["@EventType"].Value = eventType;
                            command.Parameters["@EventData"].Value = eventData;
                            command.CommandText =
                                "insert into Events (EventId, EventType, Version, EventData, AggregateId, CreationTime) VALUES (@EventId, @EventType, @Version, @EventData, @AggregateId, @CreationTime)";
                            command.ExecuteNonQuery();
                        }
                        else
                        {
                            command.Parameters.Add(new SqlParameter("@AggregateType", SqlDbType.VarChar));
                            command.Parameters.Add(new SqlParameter("@CreationTime", SqlDbType.DateTimeOffset));
                            command.Parameters["@AggregateType"].Value = aggregateQualifiedName;
                            command.Parameters["@CreationTime"].Value = creationTime;
                            command.CommandText =
                                "insert into Aggregates (AggregateId, Version, AggregateType, LastModified) VALUES (@AggregateId, 1, @AggregateType, @CreationTime)";
                            command.ExecuteNonQuery();
                            command.CommandText =
                                "insert into Events (EventId, EventType, Version, EventData, AggregateId, CreationTime) VALUES (@EventId, @EventType, 1, @EventData, @AggregateId, @CreationTime)";
                            command.Parameters.Add(new SqlParameter("@EventId", SqlDbType.UniqueIdentifier));
                            command.Parameters.Add(new SqlParameter("@EventType", SqlDbType.VarChar));
                            command.Parameters.Add(new SqlParameter("@EventData", SqlDbType.VarChar));
                            command.Parameters["@EventId"].Value = @event.EventId;
                            command.Parameters["@EventType"].Value = eventType;
                            command.Parameters["@EventData"].Value = eventData;
                            command.ExecuteNonQuery();
                        }
                        // Attempt to commit the transaction.
                        transaction.Commit();
                        Console.WriteLine("transaction successful!");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                        Console.WriteLine("  Message: {0}", ex.Message);

                        // Attempt to roll back the transaction.
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                            Console.WriteLine("  Message: {0}", ex2.Message);
                        }
                    }
                }
            }
            return false;
        }

        public List<EventSource> GetEventSources(Guid aggregateId)
        {
            var eventSources = new List<EventSource>();
            try
            {
                using (SqlConnection connection = new SqlConnection(this._connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(null, connection);
                    command.CommandText = "select * from Events where AggregateId = @AggregateId order by Version asc";
                    command.Parameters.Add(new SqlParameter("@AggregateId", SqlDbType.UniqueIdentifier));
                    command.Parameters["@AggregateId"].Value = aggregateId;
                    using (var sqlReader = command.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            eventSources.Add(new EventSource()
                            {
                                EventId = sqlReader.GetGuid(0),
                                EventType = sqlReader.GetString(1),
                                Version = sqlReader.GetInt32(2),
                                EventData = sqlReader.GetString(3),
                                AggregateId = sqlReader.GetGuid(4),
                                CreationTime = sqlReader.GetDateTimeOffset(5)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                Console.WriteLine("  Message: {0}", ex.Message);
            }
            return eventSources;
        }

        public void ScaffoldEventSourcing()
        {
            throw new NotImplementedException();
        }
    }
}
