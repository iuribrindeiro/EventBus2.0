using System.Collections.Generic;
using System.Threading.Tasks;
using EventBus.Core;
using EventBus.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventBus.RabbitMQ
{
    public sealed class RabbitMqEventSubscriber : IEventSubscriber
    {
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IRabbitConsumerInitializer _rabbitConsumerInitializer;
        private readonly ILogger<RabbitMqEventSubscriber> _logger;
        private readonly RabbitMqEventBusOptions _rabbitMqEventBusOptions;

        public RabbitMqEventSubscriber(
            ISubscriptionManager subscriptionManager,
            IRabbitMqPersistentConnection persistentConnection,
            IRabbitConsumerInitializer rabbitConsumerInitializer,
            IOptions<RabbitMqEventBusOptions> rabbitMqEventBusOptons, ILogger<RabbitMqEventSubscriber> logger)
        {
            _subscriptionManager = subscriptionManager;
            _persistentConnection = persistentConnection;
            _rabbitConsumerInitializer = rabbitConsumerInitializer;
            _logger = logger;
            _rabbitMqEventBusOptions = rabbitMqEventBusOptons.Value;
        }

        public Subscription<T> Subscribe<T>() where T : IntegrationEvent
        {
            var subscription = _subscriptionManager.AddSubscription<T>();
            SubscribeToRabbitMq(subscription);
            return subscription;
        }

        public Task StartListeningAsync()
            => _rabbitConsumerInitializer.InitializeConsumersChannelsAsync();

        private void SubscribeToRabbitMq<T>(Subscription<T> subscription) where T : IntegrationEvent
        {
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            _logger.LogInformation($"Binding queue to exchange with event {subscription.EventName}");
            using var channel = _persistentConnection.CreateModel();
            channel.QueueBind(
                queue: _rabbitMqEventBusOptions.QueueName,
                exchange: _rabbitMqEventBusOptions.ExchangeName,
                routingKey: subscription.EventName,
                arguments: new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", $"{_rabbitMqEventBusOptions.ExchangeName}_error"}
                    }
            );
            channel.QueueBind(
                queue: $"{_rabbitMqEventBusOptions.QueueName}_error",
                exchange: $"{_rabbitMqEventBusOptions.ExchangeName}_error",
                routingKey: subscription.EventName
            );
            _logger.LogInformation($"Queue successfully bound to exchange with event {subscription.EventName}");
        }
    }
}