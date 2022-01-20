using CQRS.EventSources;
using CQRS.Exceptions;
using Ecommerce.WriteModel.Inventory;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CQRS.Test.EventSources
{
    [TestFixture]
    public class SqlServerEventSourceTest
    {
        private SqlServerEventSource _sqlServerEventSource;
        private Config _config;

        [SetUp]
        public void Init()
        {
            _config = new Config();

            _sqlServerEventSource = new SqlServerEventSource(_config.SqlServerEventSource);
        }

        [TearDown]
        public void CleanUp()
        {
            _sqlServerEventSource.RemoveEventSourcing();
            _sqlServerEventSource.ScaffoldEventSourcing();
        }

        [Test]
        public void Should_CommitEvent_Successfully()
        {
            var aggregateId = Guid.NewGuid();
            var aggregateQualifiedName = typeof(CreateInventory).AssemblyQualifiedName;
            var addProductQualifiedName = typeof(AddProduct).AssemblyQualifiedName;
            var removeProductQualifiedName = typeof(RemoveProduct).AssemblyQualifiedName;
            var updateProductQualifiedName = typeof(UpdateProduct).AssemblyQualifiedName;

            //Arrange
            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics"
            };

            var addProduct1 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "IPhone 13",
                Price = 1800,
            };

            var addProduct2 = new AddProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Macbook Air 14 inch retina 256GB SSD",
                Price = 4500,
            };

            var removeProduct = new RemoveProduct()
            {
                AggregateId = aggregateId,
                ProductId = Guid.NewGuid(),
                EventId = Guid.NewGuid()
            };

            var updateProduct = new UpdateProduct()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Macbook Air 14 inch retina 512GB SSD",
                Price = 4700,
            };

            //Act
            _sqlServerEventSource.CommitEvent(aggregateQualifiedName, 0, createInventory);
            _sqlServerEventSource.CommitEvent(aggregateQualifiedName, 1, addProduct1);
            _sqlServerEventSource.CommitEvent(aggregateQualifiedName, 2, addProduct2);
            _sqlServerEventSource.CommitEvent(aggregateQualifiedName, 3, removeProduct);
            _sqlServerEventSource.CommitEvent(aggregateQualifiedName, 4, updateProduct);

            var eventSources = _sqlServerEventSource.GetEventSources(aggregateId);

            var createInventoryEventSource = eventSources[0];
            var addProductEventSource1 = eventSources[1];
            var addProductEventSource2 = eventSources[2];
            var removeEventSource = eventSources[3];
            var updateEventSource = eventSources[4];

            var expectedCreateInventory = (CreateInventory)JsonSerializer.Deserialize(createInventoryEventSource.EventData, typeof(CreateInventory));
            var expectedAddProduct1 = (AddProduct)JsonSerializer.Deserialize(addProductEventSource1.EventData, typeof(AddProduct));
            var expectedAddProduct2 = (AddProduct)JsonSerializer.Deserialize(addProductEventSource2.EventData, typeof(AddProduct));
            var expectedRemoveProduct = (RemoveProduct)JsonSerializer.Deserialize(removeEventSource.EventData, typeof(RemoveProduct));
            var expectedUpdateProduct = (UpdateProduct)JsonSerializer.Deserialize(updateEventSource.EventData, typeof(UpdateProduct));

            //Assert
            //Create Inventory
            Assert.AreEqual(createInventoryEventSource.AggregateId, createInventory.AggregateId);
            Assert.AreEqual(createInventoryEventSource.EventId, createInventory.EventId);
            Assert.AreEqual(createInventoryEventSource.EventType, aggregateQualifiedName);
            Assert.AreEqual(createInventoryEventSource.Version, 1);

            Assert.AreEqual(expectedCreateInventory.AggregateId, createInventory.AggregateId);
            Assert.AreEqual(expectedCreateInventory.EventId, createInventory.EventId);
            Assert.AreEqual(expectedCreateInventory.DateCreated, createInventory.DateCreated);
            Assert.AreEqual(expectedCreateInventory.InventoryName, createInventory.InventoryName);
            Assert.AreEqual(expectedCreateInventory.DateCreated, createInventory.DateCreated);

            //Add Product
            Assert.AreEqual(addProductEventSource1.AggregateId, addProduct1.AggregateId);
            Assert.AreEqual(addProductEventSource1.EventId, addProduct1.EventId);
            Assert.AreEqual(addProductEventSource1.EventType, addProductQualifiedName);
            Assert.AreEqual(addProductEventSource1.Version, 2);

            Assert.AreEqual(expectedAddProduct1.AggregateId, addProduct1.AggregateId);
            Assert.AreEqual(expectedAddProduct1.EventId, addProduct1.EventId);
            Assert.AreEqual(expectedAddProduct1.ProductId, addProduct1.ProductId);
            Assert.AreEqual(expectedAddProduct1.ProductName, addProduct1.ProductName);
            Assert.AreEqual(expectedAddProduct1.Price, addProduct1.Price);
            Assert.AreEqual(expectedAddProduct1.DateCreated, addProduct1.DateCreated);

            Assert.AreEqual(addProductEventSource2.AggregateId, addProduct2.AggregateId);
            Assert.AreEqual(addProductEventSource2.EventId, addProduct2.EventId);
            Assert.AreEqual(addProductEventSource2.EventType, addProductQualifiedName);
            Assert.AreEqual(addProductEventSource2.Version, 3);

            Assert.AreEqual(expectedAddProduct2.AggregateId, addProduct2.AggregateId);
            Assert.AreEqual(expectedAddProduct2.EventId, addProduct2.EventId);
            Assert.AreEqual(expectedAddProduct2.ProductId, addProduct2.ProductId);
            Assert.AreEqual(expectedAddProduct2.ProductName, addProduct2.ProductName);
            Assert.AreEqual(expectedAddProduct2.Price, addProduct2.Price);
            Assert.AreEqual(expectedAddProduct2.DateCreated, addProduct2.DateCreated);

            //Remove Product
            Assert.AreEqual(removeEventSource.AggregateId, removeProduct.AggregateId);
            Assert.AreEqual(removeEventSource.EventId, removeProduct.EventId);
            Assert.AreEqual(removeEventSource.EventType, removeProductQualifiedName);
            Assert.AreEqual(removeEventSource.Version, 4);

            Assert.AreEqual(expectedRemoveProduct.AggregateId, removeProduct.AggregateId);
            Assert.AreEqual(expectedRemoveProduct.EventId, removeProduct.EventId);
            Assert.AreEqual(expectedRemoveProduct.ProductId, removeProduct.ProductId);
            Assert.AreEqual(expectedRemoveProduct.DateCreated, removeProduct.DateCreated);

            //Update Product
            Assert.AreEqual(updateEventSource.AggregateId, updateProduct.AggregateId);
            Assert.AreEqual(updateEventSource.EventId, updateProduct.EventId);
            Assert.AreEqual(updateEventSource.EventType, updateProductQualifiedName);
            Assert.AreEqual(updateEventSource.Version, 5);

            Assert.AreEqual(expectedUpdateProduct.AggregateId, updateProduct.AggregateId);
            Assert.AreEqual(expectedUpdateProduct.EventId, updateProduct.EventId);
            Assert.AreEqual(expectedUpdateProduct.ProductId, updateProduct.ProductId);
            Assert.AreEqual(expectedUpdateProduct.ProductName, updateProduct.ProductName);
            Assert.AreEqual(expectedUpdateProduct.Price, updateProduct.Price);
            Assert.AreEqual(expectedUpdateProduct.DateCreated, updateProduct.DateCreated);
        }

        [Test]
        public void Should_ThrowSqlEventSourceException_When_Error_CommitingEvent()
        {
            //Arrange
            var aggregateQualifiedName = typeof(CreateInventory).AssemblyQualifiedName;

            var createInventory = new CreateInventory()
            {
                AggregateId = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics"
            };

            //Act & Assert
            _sqlServerEventSource.RemoveEventSourcing();

            Assert.Throws<SqlEventSourceException>(() =>
            {
                _sqlServerEventSource.CommitEvent(aggregateQualifiedName, 0, createInventory);
            });
        }


        public void Should_GetEventsSources_Successfully()
        {
            //Arrange
            var aggregateQualifiedName = typeof(CreateInventory).AssemblyQualifiedName;
            var aggregateId = Guid.NewGuid();

            var createInventory = new CreateInventory()
            {
                AggregateId = aggregateId,
                EventId = Guid.NewGuid(),
                InventoryName = "Electronics"
            };

            //Act
            _sqlServerEventSource.CommitEvent(aggregateQualifiedName, 0, createInventory);

            var eventSources = _sqlServerEventSource.GetEventSources(aggregateId);
            var createInventoryEventSource = eventSources[0];
            var expectedCreateInventory = (CreateInventory) JsonSerializer.Deserialize(createInventoryEventSource.EventData, typeof(CreateInventory));

            //Assert
            Assert.AreEqual(createInventoryEventSource.AggregateId, createInventory.AggregateId);
            Assert.AreEqual(createInventoryEventSource.EventId, createInventory.EventId);
            Assert.AreEqual(createInventoryEventSource.EventType, aggregateQualifiedName);
            Assert.AreEqual(createInventoryEventSource.Version, 1);

            Assert.AreEqual(expectedCreateInventory.AggregateId, createInventory.AggregateId);
            Assert.AreEqual(expectedCreateInventory.EventId, createInventory.EventId);
            Assert.AreEqual(expectedCreateInventory.DateCreated, createInventory.DateCreated);
            Assert.AreEqual(expectedCreateInventory.InventoryName, createInventory.InventoryName);
            Assert.AreEqual(expectedCreateInventory.DateCreated, createInventory.DateCreated);
        }

        [Test]
        public void Should_ScaffoldEventSourcing_Successfully()
        {
            //Act
            var tableExists = _sqlServerEventSource.TablesExists();

            //Assert
            Assert.IsTrue(tableExists);
        }

        [Test]
        public void Should_RemoveEventSourcing_Successfully()
        {
            //Act

            _sqlServerEventSource.RemoveEventSourcing();

            var tableExists = _sqlServerEventSource.TablesExists();

            //Assert
            Assert.IsFalse(tableExists);
        }
    }
}
