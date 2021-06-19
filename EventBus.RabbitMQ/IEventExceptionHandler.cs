using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ
{
    public interface IEventExceptionHandler
    {
        void HandleEventException(IModel consumerChannel, BasicDeliverEventArgs eventArgs, string eventName, object @event, Exception ex);
    }
}