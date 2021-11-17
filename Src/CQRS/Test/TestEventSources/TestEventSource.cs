using System;
using System.Collections.Generic;
using CQRS.Aggregates;
using CQRS.Events;
using CQRS.EventSources;

namespace CQRS.TestProcessors
{
    public class TestEventSource : ITestEventSource
    {
        public Dictionary<Guid, dynamic> Aggregates { get; }

        public TestEventSource()
        {
            Aggregates = new Dictionary<Guid, dynamic>();
        }

        public void CommitEvent<A, E>(A a, E e) where A : IRaiseEvent<E> where E : Event
        {
            if (Aggregates.ContainsKey(e.AggregateId))
                Aggregates[e.AggregateId] = a;
            else
                Aggregates.Add(e.AggregateId, a);
        }

        public dynamic GetAggregate(Guid aggregateId)
        {
            return Aggregates.ContainsKey(aggregateId) ? Aggregates[aggregateId] : null;
        }
    }
}
