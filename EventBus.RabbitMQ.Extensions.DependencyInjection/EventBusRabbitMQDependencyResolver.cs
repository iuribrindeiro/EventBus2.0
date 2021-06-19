using System;
using System.Reflection;
using EventBus.Core;
using EventBus.Core.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventBus.RabbitMQ.Extensions.DependencyInjection
{
    public static class EventBusRabbitMQDependencyResolver
    {
        public static IServiceCollection AddRabbitMqEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(Assembly.GetCallingAssembly());
            services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
            services.AddSingleton<IEventPublisher, RabbitEventPublisher>();
            services.AddSingleton<IRabbitConsumerInitializer, RabbitConsumerInitializer>();
            services.AddSingleton<IRabbitMessageReceiver, RabbitMessageReceiver>();
            services.AddSingleton<IEventSubscriber, RabbitMqEventSubscriber>();
            services.AddSingleton<IRabbitMessageHandler, RabbitMessageHandler>();
            services.AddSingleton<IConcurrentConsumerHandler, ConcurrentConsumerHandler>();
            services.AddSingleton<IEventExceptionHandler, EventExceptionHandler>();
            services.Configure<RabbitMqEventBusOptions>(configuration.GetSection("EventBus:RabbitMq"));
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                var options = sp.GetService<IOptions<RabbitMqEventBusOptions>>().Value;
                return new ConnectionFactory()
                {
                    VirtualHost = options.VirtualHost,
                    HostName = options.HostName,
                    UserName = options.Username,
                    Password = options.Password,
                    Port = options.Port,
                    ContinuationTimeout = TimeSpan.FromSeconds(30),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(30),
                    SocketReadTimeout = TimeSpan.FromSeconds(30),
                    SocketWriteTimeout = TimeSpan.FromSeconds(30),
                };
            });
            services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
            return services;
        }
    }
}