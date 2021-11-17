using CQRS.Events;
using Ecommerce.Domain;

namespace Ecommerce.Handlers
{
    public class InventoryReadModelHandler : IEventHandler,
        IHandleEvent<InventoryCreated>
    {
        private IReadModel _readModel;
        public int Priority { get; set; } = 1;

        public InventoryReadModelHandler(IReadModel readModel)
        {
            _readModel = readModel;
        }

        public void Handle(InventoryCreated e)
        {
           _readModel.Do(typeof(InventoryReadModelHandler));
        }
    }
}
