using System;

namespace EventBus.Core.Resilience
{
    public record Failure<T> where T : IntegrationEvent
    {
        public Failure(T @event, int retryAttempt, Exception exception)
        {
            Event = @event;
            RetryAttempt = retryAttempt;
            Exception = exception;
        }

        public T Event { get; }
        public int RetryAttempt { get; }
        public Exception Exception { get; }
    }
}