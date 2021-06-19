using System;

namespace EventBus.Core.Abstractions
{
    public sealed class IntegrationEventPublishRequest
    {
        public IntegrationEventPublishRequest(string eventBody, Guid eventId, string eventName, string concurrentId)
        {
            EventBody = eventBody;
            EventId = eventId;
            EventName = eventName;
            ConcurrentId = concurrentId;
        }

        public string EventBody { get; }
        public Guid EventId { get; }
        public string EventName { get; }
        public string? ConcurrentId { get; }
    }
}