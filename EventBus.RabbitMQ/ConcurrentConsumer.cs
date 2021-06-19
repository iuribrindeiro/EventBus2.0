using System;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.RabbitMQ
{
    public class ConcurrentConsumer
    {
        public IServiceScope ServiceScope { get; init; }
        public string ConcurrenceId { get; init; }
        public DateTime LastMessageTime { get; set; }
        public string ConsumerTag { get; set; }
        public bool IsProcessingMessage { get; set; }

        public System.Timers.Timer Timer { get; set; } = new System.Timers.Timer();
    }
}