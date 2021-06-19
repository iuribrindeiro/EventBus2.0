using System;
using System.Collections.Generic;
using System.Text;
using EventBus.Core;
using EventBus.Core.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ
{
    public sealed class EventExceptionHandler : IEventExceptionHandler
    {
        private readonly ILogger<EventExceptionHandler> _logger;
        private readonly ISubscriptionManager _subscriptionManager;
        private RabbitMessageReceiver _rabbitMessageReceiver;
        private readonly RabbitMqEventBusOptions _options;

        public EventExceptionHandler(ILogger<EventExceptionHandler> logger, ISubscriptionManager subscriptionManager, IOptions<RabbitMqEventBusOptions> options)
        {
            _logger = logger;
            _subscriptionManager = subscriptionManager;
            _options = options.Value;
        }

        public void HandleEventException(IModel consumerChannel, BasicDeliverEventArgs eventArgs, string eventName,
            object @event, Exception ex)
        {
            _logger.LogError(ex, "Failed to handle event");
            var subscription = _subscriptionManager.FindSubscription(eventName);
            dynamic dynamicSubscription = subscription;
            dynamic retryConfiguration = dynamicSubscription.RetryPolicyConfiguration;
            var newAttemptCount = IncrementAttempt(eventArgs);
            var failure = CreateFailure(subscription, @event, newAttemptCount, ex);
            var shouldRetry = retryConfiguration.Retry(failure);
            var retryAttemptsExceeded =
                !retryConfiguration.ForeverRetry && newAttemptCount >= retryConfiguration.MaxRetryTimes;

            if (shouldRetry && !retryAttemptsExceeded)
                PublishRetry(consumerChannel, eventArgs, eventName, @event, subscription, failure);
            else if (!retryConfiguration.DiscardEvent(failure))
                PublishToPermanentDeadLetter(consumerChannel, eventArgs, eventName, @event);

            consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
        }

        private void PublishToPermanentDeadLetter(IModel consumerChannel, BasicDeliverEventArgs eventArgs, string eventName,
            object @event)
        {
            consumerChannel.BasicPublish($"{_options.ExchangeName}_error", eventName, mandatory: true,
                body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)), basicProperties: eventArgs.BasicProperties);
        }

        private void PublishRetry(IModel consumerChannel, BasicDeliverEventArgs eventArgs, string eventName, object @event,
            ISubscription subscription, dynamic failure)
        {
            var retryWait = RetrieveRetryWaitingTime(subscription, failure);
            if (retryWait.TotalMilliseconds > 0)
            {
                _logger.LogInformation($"Re-queueing event with delay");
                PublishToWaitingDeadLetter(consumerChannel, eventArgs, retryWait, eventName, @event);
            }
            else
            {
                _logger.LogInformation($"Re-queueing event without delay");
                consumerChannel.BasicPublish(_options.ExchangeName, eventName, mandatory: true,
                    body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)),
                    basicProperties: eventArgs.BasicProperties);
            }
        }

        private TimeSpan RetrieveRetryWaitingTime(dynamic retryFunc, dynamic failure)
        {
            _logger.LogInformation("Retrieving retry waiting time");
            TimeSpan retryWait;
            try
            {
                retryWait = retryFunc != null ? retryFunc(failure) : throw new Exception("Retry Wait cannot be null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was a error calculating the waiting time. Default will be set");
                retryWait = TimeSpan.FromSeconds(5);
            }

            _logger.LogInformation($"The retry waiting will bee {retryWait.TotalMilliseconds} milliseconds");
            return retryWait;
        }

        private dynamic CreateFailure(ISubscription subscription, object @event, int attempts, Exception exception)
        {
            var eventTypeArgs = new[] { subscription.EventType };
            _logger.LogInformation("Creating failure object");
            var constructedEventType = typeof(Failure<>).MakeGenericType(eventTypeArgs);
            dynamic failure = Activator.CreateInstance(constructedEventType, @event, attempts, exception);
            return failure;
        }

        private void PublishToWaitingDeadLetter(IModel consumerChannel, BasicDeliverEventArgs eventArgs, TimeSpan retryWait, string eventName, object @event)
        {
            var deadLetterName = $"{_options.QueueName}_{retryWait.TotalSeconds}.error";
            DeclareWaitingDeadLetter(consumerChannel, retryWait, eventName, deadLetterName);
            _logger.LogInformation("Publishing to deadletter exchange");
            consumerChannel.BasicPublish(deadLetterName, eventName, mandatory: true,
                body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)), basicProperties: eventArgs.BasicProperties);
        }

        private void DeclareWaitingDeadLetter(IModel consumerChannel, TimeSpan retryWait, string eventName, string deadLetterName)
        {
            _logger.LogInformation($"Declaring waiting queue deadletter");
            var totalSecondsQueue = Convert.ToInt32(retryWait.TotalSeconds + 30);
            consumerChannel.QueueDeclare(deadLetterName,
                arguments: new Dictionary<string, object>()
                {
                    {"x-dead-letter-exchange", _options.ExchangeName},
                    {"x-message-ttl", Convert.ToInt64(retryWait.TotalMilliseconds)},
                    {"x-expires", Convert.ToInt64(TimeSpan.FromSeconds(totalSecondsQueue).TotalMilliseconds)},
                }, durable: true, exclusive: false, autoDelete: false);
            _logger.LogInformation($"Declaring waiting exchange deadletter");
            consumerChannel.ExchangeDeclare(deadLetterName, type: "topic", autoDelete: true);
            _logger.LogInformation($"Binding waiting exchange deadletter to queue");
            consumerChannel.QueueBind(deadLetterName,
                deadLetterName, eventName);
        }

        private int IncrementAttempt(BasicDeliverEventArgs eventArgs)
        {
            _logger.LogInformation("Incrementing event attempt");
            eventArgs.BasicProperties.Headers ??= new Dictionary<string, object>();
            if (eventArgs.BasicProperties.Headers.TryGetValue("attempts", out object attempts))
            {
                _logger.LogInformation("Attempt is already defined, incrementing 1");
                attempts = (int)attempts + 1;
                eventArgs.BasicProperties.Headers["attempts"] = attempts;
            }
            else
            {
                _logger.LogInformation("Attempt is not defined yet, setting to first try");
                attempts = 1;
                eventArgs.BasicProperties.Headers.Add("attempts", attempts);
            }

            var newAttempt = (int)attempts;
            _logger.LogInformation($"New retry attempt count: {newAttempt}");
            return newAttempt;
        }
    }
}