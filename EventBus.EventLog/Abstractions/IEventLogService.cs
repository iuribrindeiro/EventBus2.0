using System.Linq;
using System.Threading.Tasks;

namespace EventBus.EventLog.Abstractions
{
    public interface IEventLogService
    {
        IQueryable<IntegrationEventLog> EventLogs { get; }
        void AddEvent(IntegrationEventLog @event);
    }
}