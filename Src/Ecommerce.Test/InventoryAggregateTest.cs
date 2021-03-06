using System;
using System.Collections.Generic;
using System.Reflection;
using CQRS;
using CQRS.EventProcessors;
using CQRS.TestProcessors;
using Ecommerce.WriteModel.Inventory;
using NUnit.Framework;

namespace Ecommerce.Test
{
    [TestFixture]
    public class InventoryAggregateTest
    {
        private ITestEventProcessor _textEventProcessor;

        [SetUp]
        public void SetUp()
        {
            _textEventProcessor = new TestEventProcessor(new[] { Assembly.GetAssembly(typeof(InventoryAggregate)) }, new TestEventSource());
        }

        #region InventoryCreated
        [Test]
        public void Should_CreateInventory_When_InventoryNotCreated()
        {
            //Assign
            var aggregateId = Guid.NewGuid();

            //Assert
            _textEventProcessor.ProcessEvent(new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics"
            });
        }

        [Test]
        public void ShouldNot_CreateInventory_When_AggregateId_Empty()
        {
            //Assign
            var aggregateId = Guid.Empty;

            //Act & Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new CreateInventory()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                });
            });
        }

        [Test]
        public void ShouldNot_CreateInventory_When_InventoryAlreadyCreated()
        {
            //Assign
            var aggregateId = Guid.NewGuid();

            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new CreateInventory()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                }
            });

            //Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new CreateInventory()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                });
            });
        }
        #endregion

        #region AddProduct
        [Test]
        public void Should_AddProduct_When_InventoryCreated()
        {
            //Assign
            var aggregateId = Guid.NewGuid();

            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new CreateInventory()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                }
            });

            //Assert
            _textEventProcessor.ProcessEvent(new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "IPhone 13",
                Price = 10.4,
            });
        }

        [Test]
        public void ShouldNot_CreatedProduct_When_InventoryNotCreated()
        {
            //Assign
            var aggregateId = Guid.NewGuid();

            //Act & Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new AddProduct()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductName = "IPhone 13",
                    Price = 10.4,
                });
            });
        }
        #endregion

        #region RemoveProduct
        [Test]
        public void Should_RemoveProduct_When_ProductAdded()
        {
            //Assign
            var aggregateId = Guid.NewGuid();
            var productId = Guid.NewGuid();


            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new CreateInventory()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                },
                new AddProduct()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    ProductId = productId,
                    ProductName = "IPhone 13",
                    Price = 10.4,
                }
            });

            //Assert
            _textEventProcessor.ProcessEvent(new RemoveProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = productId,
            });
        }

        [Test]
        public void ShouldNot_RemoveProduct_When_InventoryNotCreated()
        {
            //Assign
            var aggregateId = Guid.NewGuid();

            //Act & Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new RemoveProduct()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                });
            });
        }

        [Test]
        public void ShouldNot_RemoveProduct_When_ProductNotAdded()
        {
            //Assign
            var aggregateId = Guid.NewGuid();

            //Act
            _textEventProcessor.Given(new List<dynamic>()
            {
                new CreateInventory()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    InventoryName = "Electronics"
                },
            });

            //Assert
            Assert.Throws<AggregateException>(() =>
            {
                _textEventProcessor.ProcessEvent(new RemoveProduct()
                {
                    AggregateId = aggregateId,
                    EventId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid()
                });
            });
        }
        #endregion
    }
}