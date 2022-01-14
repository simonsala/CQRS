using CQRS.Events;
using Ecommerce.WriteModel.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Communicator.Inventory
{
    public class InventoryHandler : IEventHandler,
        IHandleEvent<UpdateProduct>, IHandleEvent<RemoveProduct>
    {
        private IEmailSender _emailSender { get; set; }
        public InventoryHandler(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public int Priority => 2;

        public void Handle(UpdateProduct e)
        {
            _emailSender.SendEmail();
        }

        public void Handle(RemoveProduct e)
        {
            _emailSender.SendEmail();
        }
    }
}
