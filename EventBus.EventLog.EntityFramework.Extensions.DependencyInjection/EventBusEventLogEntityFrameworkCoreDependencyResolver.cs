using EventBus.EventLog.Abstractions;
using EventBus.EventLog.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EventBus.EventLog.EntityFramework.Extensions.DependencyInjection
{
    public static class EventBusEventLogEntityFrameworkCoreDependencyResolver
    {
        public static IServiceCollection AddEventLog<T>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction = null) where T : DbContext
        {
            services.AddDbContext<EventLogDbContext>(optionsAction);
            services.AddScoped<IIntegrationEventLogPublisher, IntegrationIntegrationEventLogPublisher>();
            services.AddScoped<IPersistentEventTransaction, EFPersistentEventTransaction>();
            services.AddScoped<IEventLogService, EventLogEFService>();
            services.AddScoped<IDbContextApplicationProvider>(
                sp => new DbContextApplicationProvider(sp.GetService<T>()));
            services.AddScoped<IEventLogDatabaseCreator>(sp => sp.GetService<EventLogDbContext>());
            return services;
        }
    }
}