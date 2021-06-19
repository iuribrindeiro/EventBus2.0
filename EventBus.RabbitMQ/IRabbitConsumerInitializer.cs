using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public interface IRabbitConsumerInitializer
    {
        Task InitializeConsumersChannelsAsync();
    }
}