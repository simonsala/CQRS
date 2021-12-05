using System;
using CQRS.Events;

namespace Ecommerce.WriteModel.Domain
{
    public class CreateInventory : Event
    {
        public string InventoryName { get; set; }
    }

    public class AddProduct : Event
    {
        public string ProductName { get; set; }
        public double Price { get; set; }
        public Guid SerialId { get; set; }
    }

    public class RemoveProduct : Event
    {
    }

    public class UpdateProduct : Event
    {
        public string ProductName { get; set; }
        public double Price { get; set; }
    }
}
