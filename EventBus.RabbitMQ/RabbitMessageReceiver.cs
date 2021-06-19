using EventBus.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OperationResult;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public sealed class RabbitMessageReceiver : IRabbitMessageReceiver
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RabbitMessageReceiver> _logger;
        private readonly RabbitMqEventBusOptions _options;
        private readonly IConcurrentConsumerHandler _concurrentConsumerHandler;
        private readonly IRabbitMessageHandler _rabbitMessageHandler;

        public RabbitMessageReceiver(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMessageReceiver> logger, 
            IOptions<RabbitMqEventBusOptions> options,
            IConcurrentConsumerHandler concurrentConsumerHandler,
            IRabbitMessageHandler rabbitMessageHandler)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _concurrentConsumerHandler = concurrentConsumerHandler;
            _rabbitMessageHandler = rabbitMessageHandler;
            _options = options.Value;
        }

        public async Task ReceiveAsync(IModel consumerChannel, BasicDeliverEventArgs eventArgs)
        {
            var eventId = eventArgs.BasicProperties.CorrelationId;
            var eventName = eventArgs.RoutingKey;
            _logger.LogInformation($"New {eventName} with id {eventId} arrived");
            using (_logger.BeginScope($"{eventId}:{eventName}"))
            {
                consumerChannel.TxSelect();
                var headers = eventArgs.BasicProperties.Headers is null ? new Dictionary<string, object>() : eventArgs.BasicProperties.Headers;
                var concurenceId = headers.ContainsKey("concurrence-id") ? Encoding.UTF8.GetString(headers["concurrence-id"] as byte[]) : null;
                if (!string.IsNullOrWhiteSpace(concurenceId))
                {
                    var queueName = $"{eventName}_{concurenceId}";
                    _concurrentConsumerHandler.ConcurrentlySubscribe(concurenceId, queueName, eventName);
                    _logger.LogInformation($"Publishing to concurrent queue {queueName}");
                    consumerChannel.BasicPublish(_options.ExchangeName, queueName, mandatory: true, eventArgs.BasicProperties, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(Encoding.UTF8.GetString(eventArgs.Body.ToArray())))));
                    consumerChannel.BasicAck(eventArgs.DeliveryTag, false);
                }
                else
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    await _rabbitMessageHandler.TryHandleEvent(consumerChannel, eventArgs, scope, eventName).ConfigureAwait(false);
                }
                consumerChannel.TxCommit();
            }

            _logger.LogInformation("Message handled!");
        }
    }
}