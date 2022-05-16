using System;
using CQRS.Events;

namespace Ecommerce.WriteModel.Inventory
{
    public class CreateInventory : Event
    {
        public string InventoryName { get; set; }
    }

    public class AddProduct : Event
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
    }

    public class RemoveProduct : Event
    {
        public Guid ProductId { get; set; }
    }

    public class UpdateProduct : Event
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
    }

    //Only used to test EventProcessor handler's retries
    public class BadEvent : Event
    {

    }
}
