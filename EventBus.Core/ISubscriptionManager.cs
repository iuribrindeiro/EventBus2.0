namespace EventBus.Core
{
    public interface ISubscriptionManager
    {
        Subscription<T> AddSubscription<T>() where T : IntegrationEvent;
        ISubscription FindSubscription(string eventName);
    }
}