using System;
using System.Threading.Tasks;

namespace EventBus.EventLog.EntityFrameworkCore
{
    public interface IPersistentEventTransaction
    {
        Task<Guid> SaveChangesWithEventLogsAsync();
    }
}