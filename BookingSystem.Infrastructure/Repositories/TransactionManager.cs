using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookingSystem.Infrastructure.Repositories;

public class TransactionManager : ITransactionManager
{
    private readonly ApplicationDbContext _context;

    public TransactionManager(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransaction(dbTransaction);
    }

    private sealed class EfTransaction : ITransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
        }
    }
}
