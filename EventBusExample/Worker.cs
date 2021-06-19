using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventBus.Core;
using EventBus.Core.Abstractions;
using MediatR;
using OperationResult;

namespace EventBusExample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ContadorService _contadorService;

        public Worker(ILogger<Worker> logger, IEventPublisher eventPublisher, IEventSubscriber eventSubscriber, ContadorService contadorService)
        {
            _logger = logger;
            _eventPublisher = eventPublisher;
            _eventSubscriber = eventSubscriber;
            _contadorService = contadorService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _eventSubscriber.Subscribe<SolicitandoGeracaoNumeroNotaEvent>();
            // for (int i = 0; i < 20; i++)
            // {
            //     var concurrenceId = new Random().Next(1, 21).ToString();
            //     await Task.WhenAll(new List<Task>
            //     {
            //         _eventPublisher.PublishAsync(new SolicitandoGeracaoNumeroNotaEvent(concurrenceId)),
            //         _eventPublisher.PublishAsync(new SolicitandoGeracaoNumeroNotaEvent(concurrenceId)),
            //         _eventPublisher.PublishAsync(new SolicitandoGeracaoNumeroNotaEvent(concurrenceId)),
            //         _eventPublisher.PublishAsync(new SolicitandoGeracaoNumeroNotaEvent(concurrenceId)),
            //         _eventPublisher.PublishAsync(new SolicitandoGeracaoNumeroNotaEvent(concurrenceId)),
            //         _eventPublisher.PublishAsync(new SolicitandoGeracaoNumeroNotaEvent(concurrenceId)),
            //     });
            // }

            await _eventPublisher.PublishAsync(new SolicitandoGeracaoNumeroNotaEvent("1"));
            await _eventSubscriber.StartListeningAsync();

            var timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 10000;
            timer.Elapsed += (s, ea) =>
            {
                _logger.LogInformation($"Count total {_contadorService.Counter}");
            };
            timer.Start();
            Console.ReadKey();
        }
    }

    public class SolicitandoGeracaoNumeroNotaEvent : IntegrationEvent
    {
        public SolicitandoGeracaoNumeroNotaEvent(string concurrenceId)
            => ConcurrenceId = concurrenceId;
    }

    public class SolicitandoGeracaoNumeroNotaEventHandler : IRequestHandler<SolicitandoGeracaoNumeroNotaEvent, Result>
    {
        private readonly ContadorService _contadorService;
        private readonly ILogger<SolicitandoGeracaoNumeroNotaEventHandler> _logger;

        public SolicitandoGeracaoNumeroNotaEventHandler(ContadorService contadorService, ILogger<SolicitandoGeracaoNumeroNotaEventHandler> logger)
        {
            _contadorService = contadorService;
            _logger = logger;
        }

        public async Task<Result> Handle(SolicitandoGeracaoNumeroNotaEvent request, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope("------------------------------------------"))
            {
                _contadorService.Numbers.Add(new Number(){ Value = ++_contadorService.Counter });
                _logger.LogInformation($"Current counter {_contadorService.Counter} from {request.ConcurrenceId}");
            }
            return Result.Success();
        }
    }
}
