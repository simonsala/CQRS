using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.ReadModel
{
    public interface IDocument
    {
       public Guid DocumentId { get; }
    }

    public class Document : IDocument
    {
        public Guid DocumentId { get; }

        public Document(Guid documentId)
        {
            DocumentId = documentId;
        }
    }
}
