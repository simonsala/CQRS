using CQRS.Events;
using Ecommerce.Domain;


namespace Ecommerce.Handlers
{
    public class DummyHandler1 : IEventHandler,
        IHandleEvent<InventoryCreated>
    {
        public int Priority => 2;

        private IFakeService _fakeService;

        public DummyHandler1(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }


        public void Handle(InventoryCreated e)
        {
            _fakeService.Do(typeof(DummyHandler1));
        }
    }
}
