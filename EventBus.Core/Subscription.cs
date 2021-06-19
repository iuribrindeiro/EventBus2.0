using System;
using EventBus.Core.Resilience;

namespace EventBus.Core
{
    public interface ISubscription
    {
        Type EventType { get; }
        string EventName { get; }
    }

    public record Subscription<T> : ISubscription where T : IntegrationEvent
    {
        public RetryPolicyConfiguration<T> RetryPolicyConfiguration { get; } = new RetryPolicyConfiguration<T>();
        public string EventName => EventType.Name;
        public Type EventType { get; } = typeof(T);
        public void OnFailure(Action<RetryPolicyConfiguration<T>> config)
            => config(RetryPolicyConfiguration);
    }
}