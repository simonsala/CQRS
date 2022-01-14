using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.ReadModel.Inventory.Models
{
    public class ProductDocument : Document
    {
        public string ProductName { get; set; }
        public double Price { get; set; }
        public Guid ProductId { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset LastModified { get; set; }

        public ProductDocument(Guid documentId): base(documentId)
        {
        }
    }
}
