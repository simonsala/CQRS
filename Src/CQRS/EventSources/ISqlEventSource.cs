using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CQRS.Events;

namespace CQRS.EventSources
{
    public interface ISqlEventSource
    {
        bool CommitEvent<E>(string aggregateQualifiedName, E @event) where E : Event;
        List<EventSource> GetEventSources(Guid aggregateId);
    }
}
