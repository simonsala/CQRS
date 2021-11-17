using System;
using CQRS.Events;

namespace Ecommerce.Domain
{
    public class InventoryCreated : Event
    {
        public string InventoryName { get; set; }
    }

    public class ProductAdded : Event
    {
        public string ProductName { get; set; }
        public double Price { get; set; }
        public Guid SerialId { get; set; }
    }

    public class ProductRemoved : Event
    {
    }

    public class ProductUpdated : Event
    {
        public string ProductName { get; set; }
        public double Price { get; set; }
    }
}
