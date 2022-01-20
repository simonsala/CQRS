using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using CQRS.Events;
using CQRS.Exceptions;

namespace CQRS.EventSources
{
    public class SqlServerEventSource : ISqlEventSource
    {
        private readonly string _connectionString;

        public SqlServerEventSource(string connectionString)
        {
            this._connectionString = connectionString;
            ScaffoldEventSourcing();
        }

        public bool CommitEvent<E>(string aggregateQualifiedName, int currentVersion, E @event) where E : Event
        {
            using var connection = new SqlConnection(this._connectionString);
            
            try
            {
                connection.Open();
                
                using var transaction = connection.BeginTransaction("StoreEventTransaction");

                try
                {
                    var command = connection.CreateCommand();

                    command.Connection = connection;
                    command.Transaction = transaction;

                    var creationTime = DateTimeOffset.Now;
                    var eventData = JsonSerializer.Serialize(@event);
                    var eventType = typeof(E).AssemblyQualifiedName;
                    var version = GetVersion(@event.AggregateId);

                    if (version > currentVersion)
                        throw new ConcurrencyException($"A concurrency issue has occurred.");

                    command.CommandText = "select Count(1) FROM Aggregates WHERE AggregateId = @AggregateId";
                    command.Parameters.Add(new SqlParameter("@AggregateId", SqlDbType.UniqueIdentifier));
                    command.Parameters["@AggregateId"].Value = @event.AggregateId;

                    var count = (int)command.ExecuteScalar();
                    if (count == 1)
                    {
                        command.CommandText = "select Version FROM Aggregates WHERE AggregateId = @AggregateId";
                        command.Parameters.Add(new SqlParameter("@Version", SqlDbType.Int));
                        command.Parameters.Add(new SqlParameter("@CreationTime", SqlDbType.DateTimeOffset));
                        command.Parameters["@Version"].Value = version + 1;
                        command.Parameters["@CreationTime"].Value = creationTime;
                        command.CommandText =
                            "update Aggregates set Version = @Version, LastModified = @CreationTime where AggregateId = @AggregateId";
                        command.ExecuteNonQuery();

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

                    transaction.Commit();
                    Console.WriteLine("transaction successful!");
                    return true;
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                        throw new SqlEventSourceException($"CommitEvent method from SqlServerEventSource. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Exception: {ex.Message}.");
                    }
                    catch (Exception ex2)
                    {
                        throw new SqlEventSourceException($"CommitEvent method from SqlServerEventSource. Transaction rollback failed. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Inner Exception: {ex.Message}. Outer Exception: {ex2.Message}");
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                throw new SqlEventSourceException($"CommitEvent method from SqlServerEventSource. Aggregate: {aggregateQualifiedName}. Event: {@event.GetType()}. Version: {currentVersion}. Exception: {ex.Message}.");
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

            using SqlConnection connection = new SqlConnection(this._connectionString);
           
            try
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
            catch (Exception ex)
            {
                throw new SqlEventSourceException($"GetEventSources method from SqlServerEventSource. AggregateId: {aggregateId}. Exception: {ex.Message}.");
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
            using SqlConnection connection = new SqlConnection(this._connectionString);

            try
            {
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.Connection = connection;
                command.CommandText = "select COUNT(TABLE_NAME) from INFORMATION_SCHEMA.TABLES where TABLE_NAME like 'Aggregates' or TABLE_NAME like 'Events'";
                
                using var sqlReader = command.ExecuteReader();

                var count = 0;

                if (sqlReader.Read())
                {
                    count = sqlReader.GetInt32(0);
                }

                var tablesExists = (count == 2);
                return tablesExists;
            }
            finally
            {
                connection.Close();
            }
        }

        public void CreateEventsTable()
        {
            using SqlConnection connection = new SqlConnection(this._connectionString);
    
            try
            {
                connection.Open();
                
                var command = connection.CreateCommand();
                command.CommandText =
                @"SET ANSI_NULLS ON

                 SET QUOTED_IDENTIFIER ON

                 CREATE TABLE [dbo].[Events](
	                [EventId] [uniqueidentifier] NOT NULL,
	                [EventType] [varchar](500) NULL,
	                [Version] [int] NOT NULL,
	                [EventData] [varchar](max) NOT NULL,
	                [AggregateId] [uniqueidentifier] NOT NULL,
	                [CreationTime] [datetimeoffset](7) NOT NULL,
                PRIMARY KEY CLUSTERED 
                (
	                [EventId] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

                ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [FK_AggregateId] FOREIGN KEY([AggregateId])
                REFERENCES [dbo].[Aggregates] ([AggregateId])

                ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_AggregateId]

                ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [EventData record should be formatted as JSON] CHECK  ((isjson([EventData])=(1)))

                ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [EventData record should be formatted as JSON]
                ";

                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        public void CreateAggregatesTable()
        {
            using SqlConnection connection = new SqlConnection(this._connectionString);

            try
            {
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.CommandText =
                @"SET ANSI_NULLS ON

                SET QUOTED_IDENTIFIER ON

                CREATE TABLE [dbo].[Aggregates] (
                  [AggregateId] [uniqueidentifier] NOT NULL,
                  [AggregateType] [varchar](500) NULL,
                  [Version] [int] NULL,
                  [LastModified] [datetimeoffset](7) NOT NULL,
                  CONSTRAINT [PK_AggregateId] PRIMARY KEY CLUSTERED
                  (
                  [AggregateId] ASC
                  ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]
                ";

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
            
            using SqlConnection connection = new SqlConnection(this._connectionString);
            
            try
            {
                connection.Open();
                
                var command = connection.CreateCommand();
                command.CommandText = "select Version FROM Aggregates WHERE AggregateId = @AggregateId";
                command.Parameters.Add(new SqlParameter("@AggregateId", SqlDbType.UniqueIdentifier));
                command.Parameters["@AggregateId"].Value = aggregateId;
                
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

            using SqlConnection connection = new SqlConnection(this._connectionString);

            try
            {
                connection.Open();

                var command = connection.CreateCommand();
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
