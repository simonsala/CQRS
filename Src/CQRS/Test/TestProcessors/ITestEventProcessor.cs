using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Events;

namespace CQRS.EventProcessors
{
    public interface ITestEventProcessor
    {
        void ProcessEvent<E>(E @event) where E : Event;

        TestEventProcessor Given(List<dynamic> @events);

        void Then(Action action);
    }
}
