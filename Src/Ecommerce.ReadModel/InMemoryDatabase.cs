using Ecommerce.ReadModel.Inventory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Ecommerce.ReadModel
{
    public interface IInMemoryDatabase
    {
       public T Get<T>(Guid documentId);
       public void Add<T>(T document);
       public void Update<T>(T document);
       public void Remove(Guid documentId);
       public Guid? GetDocumentId<T>(T document);
       public bool DocumentExists(Guid documentId);
    }

    public class InMemoryDatabase : IInMemoryDatabase
    {
        public Dictionary<Guid, object> Documents { get; }

        public InMemoryDatabase()
        {
            Documents = new Dictionary<Guid, object>();
        }

        public void Add<T>(T document)
        {
            var documentId = GetDocumentId<T>(document);
            if (documentId != null)
            {
                if (!DocumentExists(documentId.Value))
                    Documents.Add(documentId.Value, document);
            }
        }

        public void Update<T>(T document)
        {
            var documentId = GetDocumentId<T>(document);
            if (documentId != null)
            {
                if (DocumentExists(documentId.Value))
                    Documents[documentId.Value] = document;
            }
        }

        public void Remove(Guid documentId)
        {
            if (DocumentExists(documentId))
                Documents.Remove(documentId);
        }

        public Guid? GetDocumentId<T>(T document)
        {
            if (document is Document)
            {
                var documentId = ((IDocument)document).DocumentId;
                return documentId;
            }
            return null;
        }

        public bool DocumentExists(Guid documentId)
        {
            return Documents.ContainsKey(documentId);
        }

        public T Get<T>(Guid documentId)
        {
            if (DocumentExists(documentId))
            {
                return (T)Documents[documentId];
            }
            return default(T);
        }
    }
}
