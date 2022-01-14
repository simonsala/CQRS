using System;
using System.Dynamic;
using System.Text.Json;

namespace CQRS.Events
{
    public interface IHandleEvent<T> where T : Event
    {
        public void Handle(T e);
    }

    public interface IEventHandler
    {
        public int Priority { get; }
    }

    public class Event
    {
       public Guid AggregateId { get; set; }
       public Guid EventId { get; set; }
       public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
    }
}
