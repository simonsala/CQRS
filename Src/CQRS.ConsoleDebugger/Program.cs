using System;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using CQRS.EventProcessors;
using CQRS.EventSources;
using Ecommerce.Domain;
using Ecommerce.Handlers;
using Module = Autofac.Module;

namespace CQRS.ConsoleDebugger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ContainerBuilder();

            var handlerModule = new HandlerModule();

            var sqlServer = new SqlServerEventSource(
                @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ES;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            var eventProcessor = new EventProcessor(new[] { Assembly.GetAssembly(typeof(InventoryAggregate)), }, sqlServer, handlerModule);

            var aggregateId = Guid.Parse("134BADE9-19C2-4A8F-BB1F-7229CDD751B1");
            eventProcessor.ProcessEvent(new InventoryCreated()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics"
            });
        }

        public class HandlerModule : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterType(typeof(Mongo)).As<IReadModel>();
                builder.RegisterType(typeof(DumbFakeService)).As<IFakeService>();
            }
        }
    }
}
