using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CQRS.Aggregates;
using CQRS.Events;
using CQRS.EventSources;
using CQRS.Exceptions;
using AggregateException = System.AggregateException;

namespace CQRS.EventProcessors
{
    public class EventProcessor : IEventProcessor
    {
        private Assembly[] _assemblies;
        private ISqlEventSource _sqlEventSource;
        private int _retries = 3;
        private int _ongoingRetries = 0;
        private IContainer _container;

        public EventProcessor()
        {

        }

        public EventProcessor(Assembly[] assemblies, ISqlEventSource sqlEventSource, int retries, IContainer container)
        {
            _assemblies = assemblies;
            _sqlEventSource = sqlEventSource;
            _retries = retries;
            _container = container;
        }

        public ISqlEventSource SqlEventSource
        {
            get => _sqlEventSource;
            set
            {
                _sqlEventSource = value;
            }
        }

        public int Retries
        {
            get => _retries;
            set
            {
                _retries = value;
            }
        }

        public int OngoingRetries
        {
            get => _ongoingRetries;
        }

        public Assembly[] Assemblies
        {
            get => _assemblies;
            set
            {
                _assemblies = value;
            }
        }

        public IContainer Container
        {
            get => _container;
            set
            {
                _container = value;
            }
        }

        public void ProcessEvent<E>(E @event) where E : Event
        {
            var retries = 1;
            _ongoingRetries = 0;

            while (retries != _retries + 1)
            {
                try
                {
                    _ongoingRetries = retries;

                    if (ProcessDomain(@event))
                    {
                        ProcessHandlers(@event);
                    }
                    break;
                }
                catch (SqlEventSourceException)
                {
                    if (retries == _retries) throw;
                }
                catch (ConcurrencyException)
                {
                    if (retries == _retries) throw;
                }
                catch (Exception)
                {
                     throw;
                }

                retries++;
            }
        }

        public bool ProcessDomain<E>(E @event) where E : Event
        {
            var aggregateType = GetAggregateType(typeof(IRaiseEvent<E>));
            var aggregateQualifiedName = aggregateType.AssemblyQualifiedName;

            var builder = new ContainerBuilder();
            builder.RegisterType(aggregateType);

            var container = builder.Build();

            using var scope = container.BeginLifetimeScope();
            var aggregate = scope.Resolve(aggregateType);

            var eventSources = _sqlEventSource.GetEventSources(@event.AggregateId);

            var currentVersion = eventSources.Count > 0 ? eventSources.Last().Version : 0;

            if (eventSources.Count > 0)
                foreach (var eventSource in eventSources)
                {
                    var previousEventType = Type.GetType(eventSource.EventType);
                    var previousEvent = JsonSerializer.Deserialize(eventSource.EventData, previousEventType);
                    HandleAndApply(ref aggregate, previousEvent, previousEventType);
                }

            HandleAndApply(ref aggregate, @event, typeof(E));

            var eventCommitted = _sqlEventSource.CommitEvent(aggregateQualifiedName, currentVersion, @event);

            return eventCommitted;
        }

        public void ProcessHandlers<E>(E @event) where E : Event
        {
            var eventHandlerTypes = GetEventHandlerTypes(typeof(IHandleEvent<E>));

            using var scope = _container.BeginLifetimeScope(
                b =>
                {
                    foreach (var eventHandlerType in eventHandlerTypes)
                    {
                        b.RegisterType(eventHandlerType).As<IEventHandler>();
                    }
                });

            var eventHandlers =
                scope.Resolve<IEnumerable<IEventHandler>>()
                    .OrderBy(e => e.Priority);

            foreach (var eventHandler in eventHandlers)
            {
                var handleEvent = eventHandler as IHandleEvent<E>;
                handleEvent.Handle(@event);
            }
        }

        public void HandleAndApply(ref object aggregate, object @event, Type eventType)
        {
            var raiseEventType = typeof(IRaiseEvent<>);
            var aggregateHandleType = raiseEventType.MakeGenericType(eventType);

            var handleMethodInfo = aggregateHandleType.GetMethod("Handle");
            handleMethodInfo?.Invoke(aggregate, new[] { @event });

            var applyMethodInfo = aggregateHandleType.GetMethod("Apply");
            applyMethodInfo?.Invoke(aggregate, new[] { @event });
        }

        public Type GetAggregateType(Type raiseEventType)
        {
            var types = _assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsAssignableTo(raiseEventType)).ToArray();

            switch (types.Length)
            {
                case > 1:
                    throw new AggregateException("More than one aggregate handling the same event.");
                case 0:
                    throw new AggregateException("No aggregate found that handles that event.");
            }

            return types[0];
        }

        public List<Type> GetEventHandlerTypes(Type handlerEventType)
        {
            var eventHandlerTypes =
                _assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsAssignableTo(typeof(IEventHandler)) && t.IsAssignableTo(handlerEventType))
                    .ToList();
            return eventHandlerTypes;
        }

        public E ReplayAggregate<E>(Guid aggregateId)
        {
            var builder = new ContainerBuilder();
            var aggregateType = typeof(E);
            builder.RegisterType(aggregateType);
            var container = builder.Build();

            using var scope = container.BeginLifetimeScope();
            var aggregate = scope.Resolve(aggregateType);

            var eventSources = _sqlEventSource.GetEventSources(aggregateId);

            if (eventSources.Count > 0)
                foreach (var eventSource in eventSources)
                {
                    var previousEventType = Type.GetType(eventSource.EventType);
                    var previousEvent = JsonSerializer.Deserialize(eventSource.EventData, previousEventType);
                    HandleAndApply(ref aggregate, previousEvent, previousEventType);
                }

            return (E)aggregate;
        }
    }
}
