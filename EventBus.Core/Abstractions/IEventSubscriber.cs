using System.Threading.Tasks;

namespace EventBus.Core.Abstractions
{
    public interface IEventSubscriber
    {
        Subscription<T> Subscribe<T>() where T : IntegrationEvent;
        Task StartListeningAsync();
    }
}