using System;
using System.Collections.Generic;
using CQRS.Aggregates;

namespace Ecommerce.Domain
{
    public class InventoryAggregate : IAggregate,
        IRaiseEvent<InventoryCreated>,
        IRaiseEvent<ProductAdded>, 
        IRaiseEvent<ProductRemoved>, 
        IRaiseEvent<ProductUpdated>
    {
        public Guid AggregateId { get; set; }
        private List<Guid> _productIds = new();
        
        public void Handle(InventoryCreated e)
        {
            if (e.AggregateId == Guid.Empty)
                throw new AggregateException("InventoryAggregate Id can not be empty.");
            if (AggregateId == e.AggregateId)
                throw new AggregateException("InventoryAggregate has already been added.");
        }

        public void Apply(InventoryCreated e)
        {
            AggregateId = e.AggregateId;
        }

        public void Handle(ProductAdded e)
        {
            if (e.AggregateId == Guid.Empty || AggregateId != e.AggregateId)
                throw new AggregateException("Cam not add a product to an empty inventory.");
        }

        public void Apply(ProductAdded e)
        { 
            _productIds.Add(e.EventId);
        }

        public void Handle(ProductRemoved e)
        {
            if (e.AggregateId == Guid.Empty || AggregateId != e.AggregateId)
                throw new AggregateException("Cam not remove a product from an empty inventory.");
            if (!_productIds.Contains(e.EventId))
                throw new AggregateException("Product has been already removed or has not been created yet.");
        }

        public void Apply(ProductRemoved e)
        {
            _productIds.Remove(e.EventId);
        }
        
        public void Handle(ProductUpdated e)
        {
            if (e.AggregateId == Guid.Empty)
                throw new AggregateException("Cam not update a product of an empty inventory.");
            if (!_productIds.Contains(e.EventId))
                throw new AggregateException("Can not update a product that does not exist.");
        }

        public void Apply(ProductUpdated e)
        {
        }
    }
}
