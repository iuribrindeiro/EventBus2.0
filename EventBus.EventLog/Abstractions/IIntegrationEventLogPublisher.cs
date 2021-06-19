using System;
using System.Threading.Tasks;
using OperationResult;

namespace EventBus.EventLog.Abstractions
{
    public interface IIntegrationEventLogPublisher
    {
        Task<Result> PublishPendingEventLogs(Guid transactionId);
    }
}