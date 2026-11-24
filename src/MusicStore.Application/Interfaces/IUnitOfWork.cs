namespace MusicStore.Application.Interfaces;

using System.Data.Common;

/// <summary>
/// Explicit transaction boundary spanning multiple operations.
/// Replaces the legacy <c>MusicStore.Data.Repository.Infrastructure.IUnitOfWork</c>
/// (EF6 <c>DbContextTransaction</c> + implicit <c>SaveChanges</c>, and its
/// <c>Roolback</c> typo): commit means transaction commit — there is no
/// implicit change-tracking flush.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// The dedicated connection the unit of work runs on. Opened lazily on the
    /// first <see cref="BeginTransactionAsync"/>; operations that must join the
    /// transaction execute on this connection.
    /// </summary>
    DbConnection Connection { get; }

    /// <summary>The active transaction, or <c>null</c> when none is active.</summary>
    DbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Opens the connection (if needed) and begins a transaction. Subsequent
    /// operations on <see cref="Connection"/> enlist in it until commit,
    /// rollback or dispose.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the current transaction. No-op when no transaction is active.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the current transaction. No-op when no transaction is active.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
