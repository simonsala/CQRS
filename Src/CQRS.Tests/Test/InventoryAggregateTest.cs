using System;
using System.Collections.Generic;
using System.Reflection;
using CQRS;
using CQRS.EventProcessors;
using CQRS.TestProcessors;
using Ecommerce.Domain;
using NUnit.Framework;

namespace Debugger.Test
{
    [TestFixture]
    public class InventoryAggregateTest
    {
        private ITestEventProcessor _textEventProcessor;

        [SetUp]
        public void SetUp()
        {
            _textEventProcessor = new TestEventProcessor(new[] { Assembly.GetAssembly(typeof(InventoryAggregate))}, new TestEventSource() );
        }

        #region InventoryCreated
        [Test]
        public void InventoryCreated()
        {
            //Assign
            var aggregateId = Guid.NewGuid();
            var eventId = Guid.NewGuid();

            //Assert
             _textEventProcessor.ProcessEvent(new InventoryCreated()
            {
                AggregateId = aggregateId,
                EventId = eventId,
                InventoryName = "Electronics"
            });
        }

        [Test]
        public void InventoryCreated_EmptyAggregateId_Exception()
        {
            //Assign
            var aggregateId = Guid.Empty;
            var eventId = Guid.NewGuid();

            //Act & Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new InventoryCreated()
                {
                    AggregateId = aggregateId,
                    EventId = eventId,
                    InventoryName = "Electronics"
                });
            });
        }

        [Test]
        public void InventoryCreated_InventoryAlreadyCreated_Exception()
        {
            //Assign
            var aggregateId = Guid.NewGuid();
            var eventId = Guid.NewGuid();

            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new InventoryCreated()
                {
                    AggregateId = aggregateId,
                    EventId = eventId,
                    InventoryName = "Electronics"
                }
            });

            //Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new InventoryCreated()
                {
                    AggregateId = aggregateId,
                    EventId = eventId,
                    InventoryName = "Electronics"
                });
            });
        }
        #endregion

        #region ProductAdded
        [Test]
        public void ProductAdded()
        {
            //Assign
            var aggregateId = Guid.NewGuid();
         
            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new InventoryCreated()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                }
            });

            //Assert
            _textEventProcessor.ProcessEvent(new ProductAdded()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductName = "IPhone 13",
                Price = 10.4,
                SerialId = Guid.NewGuid(),
            });
        }

        [Test]
        public void ProductAdded_InventoryNotCreated_Exception()
        {
            //Assign
            var aggregateId = Guid.NewGuid();

            //Act & Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new ProductAdded()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    ProductName = "IPhone 13",
                    Price = 10.4,
                    SerialId = Guid.NewGuid(),
                });
            });
        }
        #endregion

        #region ProductRemoved
        [Test]
        public void ProductRemoved()
        {
            //Assign
            var aggregateId = Guid.NewGuid();
            var productEventId = Guid.NewGuid();


            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new InventoryCreated()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                },
                new ProductAdded()
                {
                    AggregateId = aggregateId,
                    EventId = productEventId,
                    ProductName = "IPhone 13",
                    Price = 10.4,
                    SerialId = Guid.NewGuid(),
                }
            });

            //Assert
            _textEventProcessor.ProcessEvent(new ProductRemoved()
            {
                AggregateId = aggregateId,
                EventId = productEventId,
            });
        }

        [Test]
        public void ProductRemoved_InventoryNotCreated_Exception()
        {
            //Assign
            var aggregateId = Guid.NewGuid();
            var productEventId = Guid.NewGuid();

            //Act & Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new ProductRemoved()
                {
                    AggregateId = aggregateId,
                    EventId = productEventId,
                });
            });
        }

        [Test]
        public void ProductRemoved_ProductNotAdded_Exception()
        {
            //Assign
            var aggregateId = Guid.NewGuid();
            var productEventId = Guid.NewGuid();

            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new InventoryCreated()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                },
            });

            //Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new ProductRemoved()
                {
                    AggregateId = aggregateId,
                    EventId = productEventId,
                });
            });
        }
        #endregion
    }
}
