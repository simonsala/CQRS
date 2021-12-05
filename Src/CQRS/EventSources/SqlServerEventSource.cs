using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;
using CQRS.Events;

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

        public bool CommitEvent<E>(string aggregateQualifiedName, E @event) where E : Event
        {
            using (SqlConnection connection = new SqlConnection(this._connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                var transaction = connection.BeginTransaction("StoreEventTransaction");

                command.Connection = connection;
                command.Transaction = transaction;

                var creationTime = DateTimeOffset.Now;
                var eventData = JsonSerializer.Serialize(@event);
                var eventType = typeof(E).AssemblyQualifiedName;
                var version = GetVersion(@event.AggregateId);

                try
                {
                    command.CommandText = "select Count(1) FROM Aggregates WHERE AggregateId = @AggregateId";
                    command.Parameters.Add(new SqlParameter("@AggregateId", SqlDbType.UniqueIdentifier));
                    command.Parameters["@AggregateId"].Value = @event.AggregateId;

                    var count = (int) command.ExecuteScalar();
                    if (count == 1)
                    {
                        command.CommandText = "select Version FROM Aggregates WHERE AggregateId = @AggregateId";
                        command.Parameters.Add(new SqlParameter("@Version", SqlDbType.Int));
                        command.Parameters.Add(new SqlParameter("@CreationTime", SqlDbType.DateTimeOffset));
                        command.Parameters["@Version"].Value = version + 1;
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
                finally
                {
                    connection.Close();
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
                Console.WriteLine("Message: {0}", ex.Message);
            }
            return eventSources;
        }

        public void ScaffoldEventSourcing()
        {
            if (TablesExists()) return;
            CreateAggregatesTable();
            CreateEventsTable();
        }

        private bool TablesExists()
        {
            using SqlConnection connection = new SqlConnection(this._connectionString);
            
            var tablesExists = true;
            
            try
            {
                connection.Open();
                
                var command = connection.CreateCommand();
                command.Connection = connection;
                command.CommandText = "select COUNT(TABLE_NAME) from INFORMATION_SCHEMA.TABLES where TABLE_NAME like 'Aggregates' or TABLE_NAME like 'Events'";
                
                using var sqlReader = command.ExecuteReader();

                var count = 0;

                if (sqlReader.Read())
                {
                    count = sqlReader.GetInt32(0);
                }

                tablesExists = (count == 2);
                return tablesExists;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return tablesExists;
        }

        private void CreateEventsTable()
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void CreateAggregatesTable()
        {
            using SqlConnection connection = new SqlConnection(this._connectionString);

            try
            {
                connection.Open();
                
                var command = connection.CreateCommand();
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private int GetVersion(Guid aggregateId)
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
            catch (Exception ex)
            {
                Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                Console.WriteLine("Message: {0}", ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return version;
        }
    }
}
