using System.Threading.Tasks;

namespace EventBus.EventLog.Abstractions
{
    public interface IIntegrationEventLogUnitOfWork
    {
        Task SaveChangesAsync();
    }
}