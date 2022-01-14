using CQRS.Events;
using Ecommerce.ReadModel.Inventory.Models;
using Ecommerce.WriteModel.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.ReadModel.Inventory
{
    public class InventoryReadModel : IEventHandler,
        IHandleEvent<CreateInventory>, IHandleEvent<AddProduct>,
        IHandleEvent<UpdateProduct>, IHandleEvent<RemoveProduct>
    {
        private IInMemoryDatabase _inMemoryDatabase;
        public int Priority => 1;

        public InventoryReadModel(IInMemoryDatabase inMemoryDatabase)
        {
            _inMemoryDatabase = inMemoryDatabase;
        }

        public void Handle(CreateInventory e)
        {
            var inventoryDocument = new InventoryDocument(e.AggregateId)
            {
                InventoryName = e.InventoryName
            };

            _inMemoryDatabase.Add(inventoryDocument);
        }

        public void Handle(AddProduct e)
        {
            var productDocument = new ProductDocument(e.ProductId)
            {
                ProductId = e.ProductId,
                ProductName = e.ProductName,
                DateCreated = DateTimeOffset.Now,
                Price = e.Price                
            };

            _inMemoryDatabase.Add(productDocument);
            
            var inventoryDocument = _inMemoryDatabase.Get<InventoryDocument>(e.AggregateId);
            inventoryDocument.AddProductId(productDocument.ProductId);
            _inMemoryDatabase.Update(inventoryDocument);
        }

        public void Handle(UpdateProduct e)
        {
            var productDocument = new ProductDocument(e.ProductId)
            {
                ProductId = e.ProductId,
                ProductName = e.ProductName,
                DateCreated = DateTimeOffset.Now,
                LastModified = DateTimeOffset.Now,
                Price = e.Price
            };

            _inMemoryDatabase.Update(productDocument);
        }

        public void Handle(RemoveProduct e)
        {
            _inMemoryDatabase.Remove(e.ProductId);

            var inventoryDocument = _inMemoryDatabase.Get<InventoryDocument>(e.AggregateId);
            inventoryDocument.RemoveProductId(e.ProductId);
            _inMemoryDatabase.Update(inventoryDocument);
        }
    }
}
