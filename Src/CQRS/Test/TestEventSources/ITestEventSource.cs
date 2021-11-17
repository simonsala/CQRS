using System;
using CQRS.Aggregates;
using CQRS.Events;

namespace CQRS.EventSources
{
    public interface ITestEventSource
    {
        void CommitEvent<A, E>(A a, E e) where A : IRaiseEvent<E> where E : Event;
        dynamic GetAggregate(Guid aggregateId);
    }
}
