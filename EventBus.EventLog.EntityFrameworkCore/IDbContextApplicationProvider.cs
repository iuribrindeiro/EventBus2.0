using Microsoft.EntityFrameworkCore;

namespace EventBus.EventLog.EntityFrameworkCore
{
    public interface IDbContextApplicationProvider
    {
        DbContext DbContext { get; }
    }
}