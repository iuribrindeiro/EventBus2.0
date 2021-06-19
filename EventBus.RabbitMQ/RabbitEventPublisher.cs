using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventBus.Core;
using EventBus.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace EventBus.RabbitMQ
{
    public sealed class RabbitEventPublisher : IEventPublisher
    {
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly ILogger<RabbitEventPublisher> _logger;
        private readonly RabbitMqEventBusOptions _options;

        public RabbitEventPublisher(
            IOptions<RabbitMqEventBusOptions> options,
            IRabbitMqPersistentConnection persistentConnection,
            ILogger<RabbitEventPublisher> logger)
        {
            _persistentConnection = persistentConnection;
            _logger = logger;
            _options = options.Value;
        }

        public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
        {
            var eventName = @event.GetType().Name;
            _logger.LogInformation($"Publishing {eventName} with id: {@event.Id}");
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            using var channel = _persistentConnection.CreateModel();
            var props = channel.CreateBasicProperties();
            props.CorrelationId = @event.Id.ToString();
            props.Headers = new Dictionary<string, object>
            {
                {"concurrence-id", @event.ConcurrenceId}
            };
            channel.BasicPublish(
                _options.ExchangeName,
                routingKey: eventName,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)));
            _logger.LogInformation($"Event published");
        }

        public async Task PublishAsync(IntegrationEventPublishRequest[] publishRequests)
        {
            _logger.LogInformation($"Publishing {publishRequests.Length} events");
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            using var channel = _persistentConnection.CreateModel();
            var batchPublish = channel.CreateBasicPublishBatch();
            foreach (var publishRequest in publishRequests)
            {
                var props = channel.CreateBasicProperties();
                var eventName = publishRequest.EventName;
                _logger.LogInformation($"Adding event {eventName} with id: {publishRequest.EventId} to batch");
                props.CorrelationId = publishRequest.EventId.ToString();
                props.Headers = new Dictionary<string, object>
                {
                    {"concurrence-id", publishRequest.ConcurrentId}
                };
                batchPublish
                    .Add(_options.ExchangeName,
                        routingKey: eventName,
                        mandatory: false,
                        properties: props,
                        body: Encoding.UTF8.GetBytes(publishRequest.EventBody));
            }

            batchPublish.Publish();
            _logger.LogInformation("All events were published");
        }
    }
}