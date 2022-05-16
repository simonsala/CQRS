using CQRS.Events;
using CQRS.EventSources;
using System.Reflection;
using Autofac;
using System;
using System.Threading.Tasks;

namespace CQRS.EventProcessors
{
    public interface IEventProcessor
    {
        bool ProcessEvent<E>(E @event) where E : Event;
        Task<bool> ProcessEventAsync<E>(E @event) where E : Event;
        bool ProcessHandlers<E>(E @event) where E : Event;
        E ReplayAggregate<E>(Guid aggregateId);
        ISqlEventSource SqlEventSource { get; set; }
        int Retries { get; set; }
        int OngoingDomainRetries { get; }
        int OngoingHandlerRetries { get; }
        Assembly[] Assemblies { get; set; }
        IContainer Container { get; set; }
    }
}
