using System;
using EventBus.Core;
using Newtonsoft.Json;

namespace EventBus.EventLog
{
    public class IntegrationEventLog
    {
        public IntegrationEventLog(IntegrationEvent @event)
        {
            Id = Guid.NewGuid();
            Status = IntegrationEventLogStatus.Pending;
            DateCreated = @event.Date;
            EventName = @event.Name;
            EventId = @event.Id;
            IntegrationEvent = @event;
            ConcurrenceId = @event.ConcurrenceId;
            Content = JsonConvert.SerializeObject(IntegrationEvent);
        }

        public void SetEventAsSent()
        {
            Status = IntegrationEventLogStatus.Sent;
            DateSent = DateTime.Now;
        }

        public void SetEventAsErrorSending(string error)
        {
            Status = IntegrationEventLogStatus.ErrorSending;
            Error = error;
        }

        public Guid Id { get; }
        public string Content { get; }
        public IntegrationEventLogStatus Status { get; private set; }
        public DateTime DateSent { get; private set; }
        public string Error { get; private set; }
        public Guid TransactionId { get; private set; }
        public DateTime DateCreated { get; }
        public string EventName { get; }
        public Guid EventId { get; }
        public string? ConcurrenceId { get; }
        public IntegrationEvent IntegrationEvent { get; }
    }
}
