using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ
{
    public sealed class ConcurrentConsumerHandler : IConcurrentConsumerHandler
    {
        private readonly ILogger<ConcurrentConsumerHandler> _logger;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IRabbitMessageHandler _rabbitMessageHandler;
        private readonly List<ConcurrentConsumer> _concurrentActiveConsumers = new();
        private readonly RabbitMqEventBusOptions _options;

        public ConcurrentConsumerHandler(ILogger<ConcurrentConsumerHandler> logger, IRabbitMqPersistentConnection persistentConnection, IServiceScopeFactory serviceScopeFactory, IRabbitMessageHandler rabbitMessageHandler, IOptions<RabbitMqEventBusOptions> options)
        {
            _logger = logger;
            _persistentConnection = persistentConnection;
            _serviceScopeFactory = serviceScopeFactory;
            _rabbitMessageHandler = rabbitMessageHandler;
            _options = options.Value;
        }

        public void ConcurrentlySubscribe(string concurenceId, string queueName, string eventName)
        {
            lock (_concurrentActiveConsumers)
            {
                if (!_concurrentActiveConsumers.Select(e => e.ConcurrenceId).Contains(concurenceId))
                {
                    var concurrentConsumer = new ConcurrentConsumer {ConcurrenceId = concurenceId, ServiceScope = _serviceScopeFactory.CreateScope()};
                    var concurrentModel = _persistentConnection.CreateModel();
                    DeclareConcurrentQueue(queueName, eventName, concurrentModel);
                    var consumer = new EventingBasicConsumer(concurrentModel);
                    consumer.Received += async (sender, ea) =>
                    {
                        _logger.LogInformation("Handling concurrent message");
                        lock (concurrentConsumer)
                        {
                            concurrentConsumer.LastMessageTime = DateTime.Now;
                            concurrentConsumer.IsProcessingMessage = true;
                        }

                        await _rabbitMessageHandler.TryHandleEvent(concurrentModel, ea, concurrentConsumer.ServiceScope, eventName, false).ConfigureAwait(false);

                        lock (concurrentConsumer)
                        {
                            concurrentConsumer.IsProcessingMessage = false;
                        }
                    };
                    SetAutoRemoveConsumerTimer(concurrentConsumer, concurrentModel);
                    _concurrentActiveConsumers.Add(concurrentConsumer);
                    concurrentConsumer.ConsumerTag = concurrentModel.BasicConsume(queueName, false, consumer);
                }
            }
        }

        private void DeclareConcurrentQueue(string queueName, string eventName, IModel concurrentModel)
        {
            concurrentModel.BasicQos(0, 1, false);
            concurrentModel.QueueDeclare(queueName, true, false, true,
                new Dictionary<string, object>
                {
                    { "x-single-active-consumer", true },
                    { "x-dead-letter-exchange", _options.ExchangeName },
                    { "x-dead-letter-routing-key", eventName }
                });
            concurrentModel.QueueBind(queueName, _options.ExchangeName, queueName);
        }

        private async Task ConcurrentHandleAsync(string eventName, ConcurrentConsumer concurrentConsumer,
            IModel concurrentModel, BasicDeliverEventArgs ea)
        {
            lock (concurrentConsumer)
            {
                concurrentConsumer.LastMessageTime = DateTime.Now;
                concurrentConsumer.IsProcessingMessage = true;
            }

            await _rabbitMessageHandler.TryHandleEvent(concurrentModel, ea, concurrentConsumer.ServiceScope, eventName, false).ConfigureAwait(false);

            lock (concurrentConsumer)
            {
                concurrentConsumer.IsProcessingMessage = false;
            }
        }

        private void SetAutoRemoveConsumerTimer(ConcurrentConsumer concurrentConsumer, IModel concurrentModel)
        {
            concurrentConsumer.Timer.Interval = 30000;
            concurrentConsumer.Timer.Enabled = true;
            concurrentConsumer.Timer.Elapsed += (source, ea) =>
            {
                lock (concurrentConsumer)
                {
                    if (!concurrentConsumer.IsProcessingMessage &&
                        (DateTime.Now - concurrentConsumer.LastMessageTime).TotalSeconds >= 10)
                    {
                        lock (_concurrentActiveConsumers)
                        {
                            if (_concurrentActiveConsumers.Contains(concurrentConsumer))
                            {
                                _logger.LogError("Removing unused consumer");
                                _concurrentActiveConsumers.Remove(concurrentConsumer);
                                concurrentModel.BasicCancel(concurrentConsumer.ConsumerTag);
                                concurrentConsumer.ServiceScope.Dispose();
                                concurrentConsumer.Timer.Enabled = false;
                            }
                        }
                    }
                }
            };
        }
    }
}