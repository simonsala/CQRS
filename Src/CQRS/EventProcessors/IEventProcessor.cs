using CQRS.Events;
using CQRS.EventSources;
using System.Reflection;
using Autofac;
using System;

namespace CQRS.EventProcessors
{
    public interface IEventProcessor
    {
        void ProcessEvent<E>(E @event) where E : Event;
        void ProcessHandlers<E>(E @event) where E : Event;
        E ReplayAggregate<E>(Guid aggregateId);
        ISqlEventSource SqlEventSource { get; set; }
        int Retries { get; set; }
        int OngoingRetries { get; }
        Assembly[] Assemblies { get; set; }
        IContainer Container { get; set; }
    }
}
