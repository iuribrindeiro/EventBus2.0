using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ
{
    public interface IRabbitMessageHandler
    {
        Task TryHandleEvent(IModel consumerChannel, BasicDeliverEventArgs eventArgs, IServiceScope scope, string eventName, bool moveToDeadLetterOnUnserializable = true);
    }
}