using System;
using System.Collections.Generic;
using EventBus.RabbitMQ.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventBusExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
            LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }));
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ContadorService>();
                    services.AddRabbitMqEventBus(hostContext.Configuration);
                    services.AddHostedService<Worker>();
                });
    }
    
    public class ContadorService
    {
        public int Counter { get; set; }
        public List<Number> Numbers { get; set; } = new List<Number>();
    }

    public class Number
    {
        public int Value { get; set; }
    }
}
