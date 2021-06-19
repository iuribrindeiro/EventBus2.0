using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ
{
    public interface IRabbitMessageReceiver
    {
        Task ReceiveAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs);
    }
}