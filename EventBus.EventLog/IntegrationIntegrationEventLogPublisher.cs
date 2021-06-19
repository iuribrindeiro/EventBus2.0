using System;
using System.Linq;
using System.Threading.Tasks;
using EventBus.Core;
using EventBus.Core.Abstractions;
using EventBus.EventLog.Abstractions;
using Microsoft.Extensions.Logging;
using OperationResult;

namespace EventBus.EventLog
{
    public class IntegrationIntegrationEventLogPublisher : IIntegrationEventLogPublisher
    {
        private readonly IEventLogService _eventLogService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<IntegrationIntegrationEventLogPublisher> _logger;
        private readonly IIntegrationEventLogUnitOfWork _integrationEventLogUnitOfWork;

        public IntegrationIntegrationEventLogPublisher(IEventLogService eventLogService, IEventPublisher eventPublisher,
            ILogger<IntegrationIntegrationEventLogPublisher> logger, IIntegrationEventLogUnitOfWork integrationEventLogUnitOfWork)
        {
            _eventLogService = eventLogService;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _integrationEventLogUnitOfWork = integrationEventLogUnitOfWork;
        }

        public async Task<Result> PublishPendingEventLogs(Guid transactionId)
        {
            using (_logger.BeginScope($"transactionId: {transactionId}"))
            {
                var eventLogs = RetrievePendingEventLogsInTransaction(transactionId);
                try
                {
                    await _eventPublisher.PublishAsync(eventLogs.Select(e => new IntegrationEventPublishRequest(e.Content, e.EventId, e.EventName, e.ConcurrenceId)).ToArray());
                    SetAllEventsAsSent(eventLogs);
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Failed to publish events {eventLogs.Select(e => e.EventName)} with ids {eventLogs.Select(e => e.Id)}");
                    SetAllEventsAsError(eventLogs, ex);
                    return Result.Error(ex);
                }
                finally
                {
                    await _integrationEventLogUnitOfWork.SaveChangesAsync();
                }
            }
        }

        private void SetAllEventsAsError(IntegrationEventLog[] eventLogs, Exception ex)
        {
            foreach (var eventLog in eventLogs)
                eventLog.SetEventAsErrorSending(ex.Message);
        }

        private void SetAllEventsAsSent(IntegrationEventLog[] eventLogs)
        {
            foreach (var eventLog in eventLogs)
                eventLog.SetEventAsSent();
        }

        private IntegrationEventLog[] RetrievePendingEventLogsInTransaction(Guid transactionId)
        {
            _logger.LogInformation("Retrieving pending events");
            var eventLogs = _eventLogService.EventLogs
                .Where(e => e.TransactionId == transactionId && e.Status == IntegrationEventLogStatus.Pending);
            _logger.LogInformation($"{eventLogs.Count()} pending events were retrieved");
            return eventLogs.ToArray();
        }
    }
}