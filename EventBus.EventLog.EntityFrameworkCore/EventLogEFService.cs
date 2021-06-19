using System.Linq;
using EventBus.EventLog.Abstractions;

namespace EventBus.EventLog.EntityFrameworkCore
{
    public class EventLogEFService : IEventLogService
    {
        private readonly EventLogDbContext _eventLogDbContext;

        public EventLogEFService(EventLogDbContext eventLogDbContext) => _eventLogDbContext = eventLogDbContext;

        public IQueryable<IntegrationEventLog> EventLogs => _eventLogDbContext.EventLogs;

        public void AddEvent(IntegrationEventLog @event)
            => _eventLogDbContext.EventLogs.Add(@event);
    }
}