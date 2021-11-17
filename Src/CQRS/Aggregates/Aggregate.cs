using System;

namespace CQRS.Aggregates
{
    public interface IAggregate
    {
        public Guid AggregateId { get; set; }
    }

    public interface IRaiseEvent<T>
    {
        public void Handle(T e);
        public void Apply(T e);
    }
}
