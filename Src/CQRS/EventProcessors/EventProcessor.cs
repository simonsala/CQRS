using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using CQRS.Aggregates;
using CQRS.Events;
using CQRS.EventSources;
using Module = Autofac.Module;

namespace CQRS.EventProcessors
{
    public class EventProcessor : IEventProcessor
    {
        private readonly Assembly[] _assemblies;
        private readonly ISqlEventSource _sqlEventSource;
        private readonly Module _handlerModule;

        public EventProcessor(Assembly[] assemblies, ISqlEventSource sqlEventSource, Module handlerModule)
        {
            _assemblies = assemblies;
            _sqlEventSource = sqlEventSource;
            _handlerModule = handlerModule;
        }

        public void ProcessEvent<E>(E @event) where E : Event
        {
            if (ProcessDomain(@event)) 
                ProcessHandlers(@event);
        }

        private bool ProcessDomain<E>(E @event) where E : Event
        {
            var eventCommitted = false;
            var types = _assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsAssignableTo(typeof(IRaiseEvent<E>))).ToList();
              
            switch (types.Count)
            {
                case > 1:
                    throw new AggregateException("More than one aggregate handling the same event.");
                case 0:
                    throw new AggregateException("No aggregate found that handles that event.");
            }

            var aggregateType = types.ToArray()[0];
            var aggregateQualifiedName = aggregateType.AssemblyQualifiedName;

            var builder = new ContainerBuilder();
            builder.RegisterType(aggregateType);
                
            var container = builder.Build();

            using var scope = container.BeginLifetimeScope();
            var aggregate = scope.Resolve(aggregateType);
                    
            var eventSources = _sqlEventSource.GetEventSources(@event.AggregateId);
                    
            if (eventSources.Count > 0)
                foreach (var eventSource in eventSources)
                {
                    var previousEventType = Type.GetType(eventSource.EventType);
                    var previousEvent = JsonSerializer.Deserialize(eventSource.EventData, previousEventType);
                    HandleAndApply(ref aggregate, previousEvent, previousEventType);
                }

            HandleAndApply(ref aggregate, @event, typeof(E));
                    
            eventCommitted = _sqlEventSource.CommitEvent(aggregateQualifiedName, @event);
            return eventCommitted;
        }

        private void HandleAndApply(ref object aggregate, object @event, Type eventType)
        {
            var iRiseEventType = typeof(IRaiseEvent<>);
            var aggregateHandleType = iRiseEventType.MakeGenericType(eventType);
           
            var handleMethodInfo = aggregateHandleType.GetMethod("Handle");
            handleMethodInfo?.Invoke(aggregate, new[] { @event });
            
            var applyMethodInfo = aggregateHandleType.GetMethod("Apply");
            applyMethodInfo?.Invoke(aggregate, new[] { @event });
        }

        public void ProcessHandlers<E>(E @event) where E : Event
        {
            var eventHandlerTypes =
                _assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsAssignableTo(typeof(IEventHandler)) && t.IsAssignableTo(typeof(IHandleEvent<E>)))
                    .ToList();
            if (eventHandlerTypes.Count <= 0) return;

            var builder = new ContainerBuilder();
            builder.RegisterModule(_handlerModule);

            foreach (var eventHandlerType in eventHandlerTypes)
            {
                builder.RegisterType(eventHandlerType).As<IEventHandler>();
            }

            var container = builder.Build();
            using var scope = container.BeginLifetimeScope();

            var eventHandlers =
                scope.Resolve<IEnumerable<IEventHandler>>()
                    .OrderBy(e => e.Priority);

            foreach (var eventHandler in eventHandlers)
            {
                var handleEvent = eventHandler as IHandleEvent<E>;
                handleEvent.Handle(@event);
            }
        }
    }
}
