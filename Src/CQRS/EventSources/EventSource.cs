using System;

namespace CQRS.EventSources
{
    public class EventSource
    {
        public Guid AggregateId { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; }
        public int Version { get; set; }
        public string EventData { get; set; }
        public DateTimeOffset CreationTime { get; set; }
    }
}
