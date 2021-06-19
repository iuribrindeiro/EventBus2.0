using System.Threading.Tasks;

namespace EventBus.Core.Abstractions
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent;
        Task PublishAsync(IntegrationEventPublishRequest[] integrationEvents);
    }
}