using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.ReadModel.Inventory.Models
{
    public class InventoryDocument : Document
    {
        private List<Guid> ProductIds {get;}
        public string InventoryName { get; set; }

        public InventoryDocument(Guid documentId) : base(documentId)
        {
            ProductIds = new List<Guid>();
        }

        public void AddProductId(Guid productId)
        {
            if (!ProductIdExists(productId))
            {
                ProductIds.Add(productId);
            }
        }

        public void RemoveProductId(Guid productId)
        {
            if (ProductIdExists(productId))
            {
                ProductIds.Remove(productId);
            }
        }

        public bool ProductIdExists(Guid productId)
        {
            return ProductIds.Any(x => x == productId);
        }
    }
}
