using System.Collections.Generic;
using System.Linq;

namespace EventBus.Core
{
    public sealed class SubscriptionManager : ISubscriptionManager
    {
        private readonly List<ISubscription> _subscription;

        public SubscriptionManager()
            => _subscription = new List<ISubscription>();

        public Subscription<T> AddSubscription<T>() where T : IntegrationEvent
        {
            var subscription = new Subscription<T>();
            _subscription.Add(subscription);
            return subscription;
        }

        public ISubscription FindSubscription(string eventName)
            => _subscription.FirstOrDefault(s => s.EventName == eventName);
    }
}