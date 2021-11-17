using CQRS.Events;
using Ecommerce.Domain;


namespace Ecommerce.Handlers
{
    public class DummyHandler2 : IEventHandler,
        IHandleEvent<InventoryCreated>
    {
        public int Priority  => 3;

        private IFakeService _fakeService;

        public DummyHandler2(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public void Handle(InventoryCreated e)
        {
            _fakeService.Do(typeof(DummyHandler2));
        }
    }
}
