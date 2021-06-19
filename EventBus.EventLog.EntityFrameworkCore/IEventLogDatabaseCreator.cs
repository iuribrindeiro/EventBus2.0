namespace EventBus.EventLog.EntityFrameworkCore
{
    public interface IEventLogDatabaseCreator
    {
        void EnsureDatabaseCreated();
    }
}