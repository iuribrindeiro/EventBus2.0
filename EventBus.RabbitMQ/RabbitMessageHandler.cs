using System;
using System.Text;
using System.Threading.Tasks;
using EventBus.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OperationResult;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ
{
    public sealed class RabbitMessageHandler : IRabbitMessageHandler
    {
        private readonly ILogger<RabbitMessageHandler> _logger;
        private readonly IEventExceptionHandler _eventExceptionHandler;
        private readonly ISubscriptionManager _subscriptionManager;

        public RabbitMessageHandler(ILogger<RabbitMessageHandler> logger, IEventExceptionHandler eventExceptionHandler, ISubscriptionManager subscriptionManager)
        {
            _logger = logger;
            _eventExceptionHandler = eventExceptionHandler;
            _subscriptionManager = subscriptionManager;
        }

        public async Task TryHandleEvent(IModel consumerChannel, BasicDeliverEventArgs eventArgs, IServiceScope scope, string eventName, bool moveToDeadLetterOnUnserializable = true)
        {
            if (TryRetriveEventType(eventName, out Type eventType) &&
                TryDeserializeEvent(eventArgs, eventType, out object @event))
            {
                try
                {
                    var mediator = scope.ServiceProvider.GetService<IMediator>();
                    var result = await mediator.Send(@event as IRequest<Result>);
                    if (result.IsSuccess)
                    {
                        _logger.LogInformation($"Event successfully handled");
                        consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
                        _logger.LogInformation($"Event removed from queue");
                    }
                    else
                        _eventExceptionHandler.HandleEventException(consumerChannel, eventArgs, eventName, @event, result.Exception);
                }
                catch (Exception ex)
                {
                    _eventExceptionHandler.HandleEventException(consumerChannel, eventArgs, eventName, @event, ex);
                }
            }
            else
            {
                _logger.LogError($"Removing unreadable event from queue");
                if (moveToDeadLetterOnUnserializable)
                    consumerChannel.BasicNack(eventArgs.DeliveryTag, false, false);
                else
                    consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
            }

            _logger.LogInformation($"Finishing handling event");
        }

        private bool TryDeserializeEvent(BasicDeliverEventArgs eventArgs, Type eventType, out object @event)
        {
            _logger.LogInformation($"Trying to deserialize event");
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            try
            {
                @event = JsonConvert.DeserializeObject(message, eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to deserialize event with body: {message}");
                @event = null;
                return false;
            }

            _logger.LogInformation($"Event was deserialized with body: {message}");
            return true;
        }

        private bool TryRetriveEventType(string eventName, out Type eventType)
        {
            _logger.LogInformation($"Trying to find event subscription");
            var eventSubscription = _subscriptionManager.FindSubscription(eventName);
            eventType = eventSubscription?.EventType;

            var subscriptionFound = eventType != null;

            if (subscriptionFound)
                _logger.LogInformation($"Event subscription found for type: {eventType.FullName}");
            else
                _logger.LogError($"Event subscription was not found");

            return subscriptionFound;
        }
    }
}