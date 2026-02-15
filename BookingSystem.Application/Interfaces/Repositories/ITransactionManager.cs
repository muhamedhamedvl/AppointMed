namespace BookingSystem.Application.Interfaces.Repositories;

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface ITransactionManager
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
