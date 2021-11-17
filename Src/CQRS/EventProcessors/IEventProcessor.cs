using System.Threading.Tasks;
using CQRS.Events;

namespace CQRS.EventProcessors
{
    public interface IEventProcessor
    {
        void ProcessEvent<E>(E @event) where E : Event;
        void ProcessHandlers<E>(E @event) where E : Event;
    }
}
