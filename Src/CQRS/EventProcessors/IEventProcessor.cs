using System.Threading.Tasks;
using CQRS.Events;
using CQRS.EventSources;
using System.Reflection;
using Module = Autofac.Module;
using Autofac;

namespace CQRS.EventProcessors
{
    public interface IEventProcessor
    {
        void ProcessEvent<E>(E @event) where E : Event;
        void ProcessHandlers<E>(E @event) where E : Event;
        ISqlEventSource SqlEventSource { get; set; }
        int Retries { get; set; }
        int OngoingRetries { get; }

        Assembly[] Assemblies { get; set; }
        IContainer Container { get; set; }
    }
}
