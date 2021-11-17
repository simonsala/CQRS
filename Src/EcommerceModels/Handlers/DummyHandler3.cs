using CQRS.Events;
using Ecommerce.Domain;


namespace Ecommerce.Handlers
{
    public class DummyHandler3 : IEventHandler,
        IHandleEvent<InventoryCreated>
    {
        public int Priority => 4;

        private IFakeService _fakeService;

        public DummyHandler3(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public void Handle(InventoryCreated e)
        {
            _fakeService.Do(typeof(DummyHandler3));
        }
    }
}

