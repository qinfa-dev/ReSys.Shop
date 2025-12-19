
using System.Data;

namespace ReSys.Core.Feature.Common.Persistence.Interfaces;

/// <summary>
/// Coordinates database operations and manages transaction boundaries.
/// Single point of entry for all save and transaction operations.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IApplicationDbContext Context { get; }

    // Persistence
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Transaction Management
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    // Transaction State
    bool HasActiveTransaction { get; }
    Guid? CurrentTransactionId { get; }

    // Raw SQL Execution
    Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default, params object[] parameters);

    // Transactional Execution
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<Task> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
}