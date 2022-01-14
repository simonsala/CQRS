using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CQRS.Events;

namespace CQRS.EventSources
{
    public interface ISqlEventSource
    {
        bool CommitEvent<E>(string aggregateQualifiedName,int currentVersion, E @event) where E : Event;
        List<EventSource> GetEventSources(Guid aggregateId);
        void ScaffoldEventSourcing();
    }
}
