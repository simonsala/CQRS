﻿using System;
using System.Collections.Generic;
using CQRS.Aggregates;

namespace Ecommerce.WriteModel.Inventory
{
    public class InventoryAggregate : IAggregate,
        IRaiseEvent<CreateInventory>,
        IRaiseEvent<AddProduct>,
        IRaiseEvent<RemoveProduct>,
        IRaiseEvent<UpdateProduct>,
        IRaiseEvent<BadEvent>
    {
        public Guid AggregateId { get; set; }
        private List<Guid> _productIds = new();
        public List<Guid> ProductIds { get => _productIds; }
        
        public void Handle(CreateInventory e)
        {
            if (e.AggregateId == Guid.Empty)
                throw new AggregateException("InventoryAggregate Id can not be empty.");
            if (AggregateId == e.AggregateId)
                throw new AggregateException("InventoryAggregate has already been created.");
        }

        public void Apply(CreateInventory e)
        {
            AggregateId = e.AggregateId;
        }

        public void Handle(AddProduct e)
        {
            if (e.AggregateId == Guid.Empty || AggregateId != e.AggregateId)
                throw new AggregateException("Cam not add a product to an empty inventory.");
        }

        public void Apply(AddProduct e)
        { 
            _productIds.Add(e.ProductId);
        }

        public void Handle(RemoveProduct e)
        {
            if (e.AggregateId == Guid.Empty || AggregateId != e.AggregateId)
                throw new AggregateException("Cam not remove a product from an empty inventory.");
            if (!_productIds.Contains(e.ProductId))
                throw new AggregateException("Product has been already removed or has not been created yet.");
        }

        public void Apply(RemoveProduct e)
        {
            _productIds.Remove(e.ProductId);
        }
        
        public void Handle(UpdateProduct e)
        {
            if (e.AggregateId == Guid.Empty)
                throw new AggregateException("Cam not update a product of an empty inventory.");
            if (!_productIds.Contains(e.ProductId))
                throw new AggregateException("Can not update a product that does not exist.");
        }

        public void Apply(UpdateProduct e)
        {
        }

        public void Handle(BadEvent e)
        {
        }

        public void Apply(BadEvent e)
        {
        }
    }
}
