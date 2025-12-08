using Ardalis.GuardClauses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using ReSys.Core.Feature.Common.Persistence.Interfaces;

using Serilog;

using IsolationLevel = System.Data.IsolationLevel;

namespace ReSys.Infrastructure.Persistence.Contexts;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly SemaphoreSlim _transactionLock = new(initialCount: 1, maxCount: 1);
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = Guard.Against.Null(input: context);
        Log.Debug(messageTemplate: "UnitOfWork initialized");
    }

    public IApplicationDbContext Context => _context;
    public bool HasActiveTransaction => _transaction != null;
    public Guid? CurrentTransactionId => _transaction?.TransactionId;

    /// <summary>
    /// Saves changes to the database. Works with or without an active transaction.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var changes = await _context.SaveChangesAsync(cancellationToken: cancellationToken);
            Log.Debug(messageTemplate: "Saved {Count} changes to database (Transaction: {HasTransaction})",
                propertyValue0: changes,
                propertyValue1: HasActiveTransaction);
            return changes;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, messageTemplate: "Failed to save changes");
            throw;
        }
    }

    /// <summary>
    /// Executes an action within a transaction scope. Automatically commits on success or rolls back on failure.
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var wasTransactionStartedHere = false;

        try
        {
            // Only start a new transaction if one doesn't exist
            if (!HasActiveTransaction)
            {
                await BeginTransactionAsync(isolationLevel: isolationLevel, cancellationToken: cancellationToken);
                wasTransactionStartedHere = true;
            }

            var result = await action();

            // Only commit if we started the transaction
            if (wasTransactionStartedHere)
            {
                await CommitTransactionAsync(cancellationToken: cancellationToken);
            }

            return result;
        }
        catch
        {
            // Only rollback if we started the transaction
            if (wasTransactionStartedHere && HasActiveTransaction)
            {
                await RollbackTransactionAsync(cancellationToken: cancellationToken);
            }
            throw;
        }
    }

    /// <summary>
    /// Executes an action within a transaction scope. Automatically commits on success or rolls back on failure.
    /// </summary>
    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await action();
            return true;
        }, isolationLevel, cancellationToken);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => BeginTransactionAsync(isolationLevel: IsolationLevel.ReadCommitted, cancellationToken: cancellationToken);

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _transactionLock.WaitAsync(cancellationToken: cancellationToken);
        try
        {
            if (_transaction != null)
            {
                Log.Warning(messageTemplate: "Transaction already active: {TransactionId}",
                    propertyValue: _transaction.TransactionId);
                throw new InvalidOperationException(message: $"Transaction already in progress: {_transaction.TransactionId}");
            }

            _transaction = await _context.Database.BeginTransactionAsync(
                isolationLevel: isolationLevel,
                cancellationToken: cancellationToken);

            Log.Information(messageTemplate: "Transaction started: {TransactionId} with {IsolationLevel}",
                propertyValue0: _transaction.TransactionId,
                propertyValue1: isolationLevel);
        }
        finally
        {
            _transactionLock.Release();
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _transactionLock.WaitAsync(cancellationToken: cancellationToken);
        try
        {
            if (_transaction == null)
                throw new InvalidOperationException(message: "No active transaction to commit");

            var transactionId = _transaction.TransactionId;

            await _transaction.CommitAsync(cancellationToken: cancellationToken);
            Log.Information(messageTemplate: "Transaction committed: {TransactionId}",
                propertyValue: transactionId);
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, messageTemplate: "Failed to commit transaction");
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
            _transactionLock.Release();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _transactionLock.WaitAsync(cancellationToken: cancellationToken);
        try
        {
            if (_transaction == null)
            {
                Log.Debug(messageTemplate: "No active transaction to rollback");
                return;
            }

            var transactionId = _transaction.TransactionId;

            await _transaction.RollbackAsync(cancellationToken: cancellationToken);
            Log.Information(messageTemplate: "Transaction rolled back: {TransactionId}",
                propertyValue: transactionId);
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, messageTemplate: "Failed to rollback transaction");
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
            _transactionLock.Release();
        }
    }

    public async Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default, params object[] parameters)
    {
        ThrowIfDisposed();
        Guard.Against.NullOrWhiteSpace(input: sql);

        try
        {
            var result = await _context.Database.ExecuteSqlRawAsync(
                sql: sql,
                parameters: parameters,
                cancellationToken: cancellationToken);

            Log.Debug(messageTemplate: "Executed SQL: {RowsAffected} rows affected",
                propertyValue: result);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, messageTemplate: "Failed to execute SQL");
            throw;
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(condition: _disposed, instance: this);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        _transactionLock.Dispose();
        _disposed = true;

        Log.Debug(messageTemplate: "UnitOfWork disposed");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_transaction != null)
            await _transaction.DisposeAsync();

        _transactionLock.Dispose();
        _disposed = true;

        Log.Debug(messageTemplate: "UnitOfWork disposed asynchronously");
    }
}