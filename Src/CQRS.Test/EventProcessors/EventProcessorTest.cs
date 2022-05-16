using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using CQRS.EventProcessors;
using CQRS.EventSources;
using CQRS.Exceptions;
using CQRS.Test.EventSources;
using Ecommerce.Communicator.Inventory;
using Ecommerce.ReadModel.Inventory;
using Ecommerce.WriteModel.Inventory;
using NUnit.Framework;
using AggregateException = CQRS.Exceptions.AggregateException;

namespace CQRS.Test.EventProcessors
{
    [TestFixture]
    public class EventProcessorTest
    {
        private ISqlEventSource _sqlEventSource;
        private IEventProcessor _eventProcessor;
        private Config _config;


        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new HandlersModule());

            var container = builder.Build();

            _config = new Config();

            _sqlEventSource = new SqliteEventSource(_config.SqliteEventSource);
            _sqlEventSource.ScaffoldEventSourcing();

            _eventProcessor = new EventProcessor();
            _eventProcessor.Retries = 2;
            _eventProcessor.SqlEventSource = _sqlEventSource;
            _eventProcessor.Assemblies = new[] { Assembly.GetAssembly(typeof(InventoryAggregate)),
                Assembly.GetAssembly(typeof(InventoryReadModel)), Assembly.GetAssembly(typeof(InventoryHandler)) };
            _eventProcessor.Container = container;
        }

        [TearDown]
        public void CleanUp()
        {
            _sqlEventSource.RemoveEventSourcing();
            _sqlEventSource.ScaffoldEventSourcing();
        }

        [Test]
        public void Should_Process_Event_Sucessfully()
        {
            //Arrange
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();
            var productId2 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            var addProduct1 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId1,
                ProductName = "IPhone 13",
                Price = 1800,
            };

            var addProduct2 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId2,
                ProductName = "Macbook Air 14 inch retina 256GB SSD",
                Price = 4500,
            };

            var removeProduct = new RemoveProduct()
            {
                AggregateId = aggregateId,
                ProductId = productId1,
                EventId = Guid.NewGuid()
            };

            var updateProduct = new UpdateProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId2,
                ProductName = "Macbook Air 14 inch retina 512GB SSD",
                Price = 4700,
            };

            //Act && Assert
            Assert.DoesNotThrow(() =>
            {
                _eventProcessor.ProcessEvent(createInventory);
                _eventProcessor.ProcessEvent(addProduct1);
                _eventProcessor.ProcessEvent(addProduct2);
                _eventProcessor.ProcessEvent(removeProduct);
                _eventProcessor.ProcessEvent(updateProduct);
            });
        }

        [Test]
        public void Should_Process_Handlers_Sucessfully()
        {
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();
            var productId2 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            var addProduct1 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId1,
                ProductName = "IPhone 13",
                Price = 1800,
            };

            var addProduct2 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId2,
                ProductName = "Macbook Air 14 inch retina 256GB SSD",
                Price = 4500,
            };

            var removeProduct = new RemoveProduct()
            {
                AggregateId = aggregateId,
                ProductId = productId1,
                EventId = Guid.NewGuid()
            };

            var updateProduct = new UpdateProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId2,
                ProductName = "Macbook Air 14 inch retina 512GB SSD",
                Price = 4700,
            };

            //Act && Assert
            Assert.DoesNotThrow(() =>
            {
                _eventProcessor.ProcessHandlers(createInventory);
                _eventProcessor.ProcessHandlers(addProduct1);
                _eventProcessor.ProcessHandlers(addProduct2);
                _eventProcessor.ProcessHandlers(removeProduct);
                _eventProcessor.ProcessHandlers(updateProduct);
            });
        }

        [Test]
        public void Should_RetryProcessingEvent_When_Exception()
        {
            //Arrange
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            _eventProcessor.SqlEventSource = new BadSqlEventSource(() => throw new SqlEventSourceException("SqlEventSourceException test"));

            Assert.Throws<SqlEventSourceException>(() =>
            {
                _eventProcessor.ProcessEvent(createInventory);
            });

            Assert.AreEqual(_eventProcessor.OngoingDomainRetries, _eventProcessor.Retries);
        }

        [Test]
        public void Should_RetryProcessingHandler_When_Exception()
        {
            //Arrange
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();

            var badEvent = new BadEvent()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
            };

            Assert.Throws<HandlerException>(() =>
            {
                _eventProcessor.ProcessEvent(badEvent);
            });

            Assert.AreEqual(_eventProcessor.OngoingHandlerRetries, _eventProcessor.OngoingHandlerRetries);
        }

        [Test]
        public void ShouldNot_ProcessEvent_When_SqlEventSourceException()
        {
            //Arrange
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            _eventProcessor.SqlEventSource = new BadSqlEventSource(() => throw new SqlEventSourceException("SqlEventSourceException test"));

            Assert.Throws<SqlEventSourceException>(() =>
            {
                _eventProcessor.ProcessEvent(createInventory);
            });
        }

        [Test]
        public void ShouldNot_ProcessEvent_When_ConcurrecyException()
        {
            //Arrange
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            _eventProcessor.SqlEventSource = new BadSqlEventSource(() => throw new ConcurrencyException("ConcurrencyException test"));

            Assert.Throws<ConcurrencyException>(() =>
            {
                _eventProcessor.ProcessEvent(createInventory);
            });
        }

        [Test]
        public void Should_ProcessEvent_When_AggregateException()
        {
            //Arrange
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            _eventProcessor.SqlEventSource = new BadSqlEventSource(() => throw new AggregateException("AggregateException test"));

            Assert.Throws<AggregateException>(() =>
            {
                _eventProcessor.ProcessEvent(createInventory);
            });
        }

        [Test]
        public void Should_ProcessEvent_When_Exception()
        {
            //Arrange
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            _eventProcessor.SqlEventSource = new BadSqlEventSource(() => throw new Exception("Exception test"));

            Assert.Throws<Exception>(() =>
            {
                _eventProcessor.ProcessEvent(createInventory);
            });
        }

        [Test]
        public void Should_ReplayAggregate_Successfully()
        {
            var aggregateId = Guid.NewGuid();
            var productId1 = Guid.NewGuid();
            var productId2 = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics",
            };

            var addProduct1 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId1,
                ProductName = "IPhone 13",
                Price = 1800,
            };

            var addProduct2 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId2,
                ProductName = "Macbook Air 14 inch retina 256GB SSD",
                Price = 4500,
            };

            //Act
            _eventProcessor.ProcessEvent(createInventory);
            _eventProcessor.ProcessEvent(addProduct1);
            _eventProcessor.ProcessEvent(addProduct2);

            var aggregate = _eventProcessor.ReplayAggregate<InventoryAggregate>(aggregateId);

            //Assert
            Assert.IsTrue(aggregate.ProductIds.Contains(productId1));
            Assert.IsTrue(aggregate.ProductIds.Contains(productId2));
        }
    }
}