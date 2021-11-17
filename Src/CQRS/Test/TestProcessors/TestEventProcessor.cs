using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using CQRS.Aggregates;
using CQRS.EventProcessors;
using CQRS.Events;
using CQRS.EventSources;

namespace CQRS
{
    public class TestEventProcessor : ITestEventProcessor
    {
        private Assembly[] _assemblies;
        private ITestEventSource _testEventSource;

        public TestEventProcessor(Assembly[] assemblies, ITestEventSource testEventSource)
        {
            _assemblies = assemblies;
            _testEventSource = testEventSource;
        }

        public TestEventProcessor Given(List<dynamic> @events)
        {
            foreach (var @event in @events)
            {
               ProcessEvent(@event);
            }
            return this;
        }

        public void Then(Action action)
        {
            action();
        }

        public void ProcessEvent<E>(E @event) where E : Event
        {
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

            var builder = new ContainerBuilder();
            builder.RegisterType(aggregateType).As<IAggregate>();
            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
               var aggregate = (IRaiseEvent<E>)_testEventSource.GetAggregate(@event.AggregateId)
                                ?? (IRaiseEvent<E>) scope.Resolve<IAggregate>();
                aggregate.Handle(@event);
                aggregate.Apply(@event);
                _testEventSource.CommitEvent(aggregate, @event);
            }
        }
    }
}
