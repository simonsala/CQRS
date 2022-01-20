using CQRS.Events;
using CQRS.EventSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Test.EventSources
{
    public class BadSqlEventSource : ISqlEventSource
    {
        public Action ThrowExceptionDelegate { get; set;}

        public BadSqlEventSource(Action throwExceptionDelegate)
        {
            ThrowExceptionDelegate = throwExceptionDelegate;
        }

        public bool CommitEvent<E>(string aggregateQualifiedName, int currentVersion, E @event) where E : Event
        {
            ThrowExceptionDelegate();
            return true;
        }

        public List<EventSource> GetEventSources(Guid aggregateId)
        {
            return new List<EventSource>();
        }

        public void ScaffoldEventSourcing()
        {
            throw new NotImplementedException();
        }

        public void RemoveEventSourcing()
        {
            throw new NotImplementedException();
        }
    }
}
