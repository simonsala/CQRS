using Autofac;
using Ecommerce.Communicator;
using Ecommerce.ReadModel;

namespace CQRS.Test.EventProcessors
{
    public class HandlersModule : Module 
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<InMemoryDatabase>().As<IInMemoryDatabase>().SingleInstance();
            builder.RegisterType<EmailSender>().As<IEmailSender>().SingleInstance();
        }
    }
}
