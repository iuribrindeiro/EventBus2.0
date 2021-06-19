using System;
using System.Linq;
using System.Threading.Tasks;
using EventBus.EventLog.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EventBus.EventLog.EntityFrameworkCore
{
    public class EventLogDbContext : DbContext, IEventLogDatabaseCreator, IIntegrationEventLogUnitOfWork
    {
        public EventLogDbContext(DbContextOptions<EventLogDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IntegrationEventLog>().HasKey(p => p.Id);
            modelBuilder.Entity<IntegrationEventLog>().Ignore(p => p.IntegrationEvent);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<IntegrationEventLog> EventLogs { get; set; }

        public Task SaveChangesAsync() => SaveChangesAsync();

        private void SetCurrentTransactionId(Guid transactionId)
        {
            var eventLogs = ChangeTracker.Entries()
                .Where(e => e.Entity is IntegrationEventLog eventLog &&
                            eventLog.TransactionId == Guid.Empty);
            foreach (var eventLog in eventLogs)
                eventLog.CurrentValues["TransactionId"] = transactionId;
        }

        public Task<int> SaveChangesAsync(Guid transactionId)
        {
            SetCurrentTransactionId(transactionId);
            return base.SaveChangesAsync();
        }

        public void EnsureDatabaseCreated()
            => Database.EnsureCreated();
    }
}