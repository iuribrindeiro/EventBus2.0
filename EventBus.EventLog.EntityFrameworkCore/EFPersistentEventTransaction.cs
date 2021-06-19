using System;
using System.Threading.Tasks;
using EventBus.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventBus.EventLog.EntityFrameworkCore
{
    public class EFPersistentEventTransaction : IPersistentEventTransaction
    {
        private readonly IDbContextApplicationProvider _dbContextApplicationProvider;
        private readonly EventLogDbContext _eventLogDbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<EFPersistentEventTransaction> _logger;

        public EFPersistentEventTransaction(
            IDbContextApplicationProvider dbContextApplicationProvider,
            EventLogDbContext eventLogDbContext, IEventPublisher eventPublisher, ILogger<EFPersistentEventTransaction> logger)
        {
            _dbContextApplicationProvider = dbContextApplicationProvider;
            _eventLogDbContext = eventLogDbContext;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<Guid> SaveChangesWithEventLogsAsync()
        {
            var strategy = _dbContextApplicationProvider.DbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = _dbContextApplicationProvider.DbContext.Database.BeginTransaction();
                await Task.WhenAll(
                    _dbContextApplicationProvider.DbContext.SaveChangesAsync(),
                    _eventLogDbContext.SaveChangesAsync(transaction.TransactionId));
                transaction.Commit();
                return transaction.TransactionId;
            });
        }
    }
}