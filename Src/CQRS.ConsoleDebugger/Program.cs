using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Autofac;
using CQRS.EventProcessors;
using CQRS.Events;
using CQRS.EventSources;
using Ecommerce.ReadModel;
using Ecommerce.ReadModel.Inventory.Models;
using Ecommerce.WriteModel.Inventory;
using Module = Autofac.Module;

namespace CQRS.ConsoleDebugger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var builder = new ContainerBuilder();

            //var handlerModule = new HandlerModule();

            //var sqlServer = new SqlServerEventSource(
            //     @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ES;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            //var sqliteServer = new SQLiteEventSource("");

            //var eventProcessor = new EventProcessor(new[] { Assembly.GetAssembly(typeof(InventoryAggregate)), }, sqlServer, 2, new HandlerModule());

            //var aggregateId = Guid.Parse("134BADE9-19C2-4A8F-BB1F-7229CDD751B1");
            //eventProcessor.ProcessEvent(new InventoryCreated()
            //{
            //    AggregateId = aggregateId,
            //    EventId = Guid.NewGuid(),
            //    InventoryName = "Electronics"
            //});

            //var @event = new CreateInventory()
            //{
            //    AggregateId = Guid.NewGuid(),
            //    EventId = Guid.NewGuid(),
            //    InventoryName = "Electronics"
            //};

            //var sqliteEventSource = new SQLiteEventSource();
            //sqliteEventSource.ScaffoldEventSourcing();
            //sqliteEventSource.CommitEvent(typeof(InventoryAggregate).AssemblyQualifiedName, 0, @event);
            //var eventSources = sqliteEventSource.GetEventSources(@event.AggregateId);
            //foreach(var e in eventSources)
            //{
            //    Console.WriteLine(JsonSerializer.Serialize(e));
            //}

            //var inMemoryDb = new InMemoryDatabase();

            //var documentId = Guid.NewGuid();

            //var inventoryDocument = new InventoryDocument(documentId)
            //{
            //    InventoryName = "Electronics"
            //};

            //inMemoryDb.Add(inventoryDocument);

            //var inventoryDoc = inMemoryDb.Get<InventoryDocument>(documentId);
            var assembly = Assembly.GetAssembly(typeof(InventoryAggregate));

            foreach(var i in   assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(Event))))
            {
                Console.WriteLine(i.Name);
            }

            //.SelectMany(a => a.GetTypes())
            //        .Where(t => t.IsAssignableTo(typeof(IEventHandler)) && t.IsAssignableTo(handlerEventType))
            //        .ToList();
        }

        public class HandlerModule : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
  
            }
        }
    }
}
