namespace MusicStore.Infrastructure.UnitOfWork;

using System.Data.Common;
using MusicStore.Application.Interfaces;
using Npgsql;

/// <summary>
/// <see cref="IUnitOfWork"/> over a dedicated <see cref="NpgsqlConnection"/>
/// (from the pooled <see cref="NpgsqlDataSource"/>) and an
/// <see cref="NpgsqlTransaction"/>. Replaces the legacy EF6
/// <c>UnitOfWork</c>: commit is a transaction commit — no implicit
/// <c>SaveChanges</c> — and rollback is spelled correctly.
/// </summary>
public sealed class NpgsqlUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlDataSource dataSource;
    private NpgsqlConnection? connection;
    private NpgsqlTransaction? transaction;
    private bool disposed;

    public NpgsqlUnitOfWork(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public DbConnection Connection => this.connection ?? throw new InvalidOperationException(
        "The unit of work connection is not open. Call BeginTransactionAsync first.");

    public DbTransaction? CurrentTransaction => this.transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        if (this.transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active on this unit of work.");
        }

        this.connection ??= await this.dataSource.OpenConnectionAsync(cancellationToken);
        this.transaction = await this.connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (this.transaction is null)
        {
            return;
        }

        await this.transaction.CommitAsync(cancellationToken);
        await this.transaction.DisposeAsync();
        this.transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (this.transaction is null)
        {
            return;
        }

        await this.transaction.RollbackAsync(cancellationToken);
        await this.transaction.DisposeAsync();
        this.transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        // Dispose without commit => implicit rollback (transaction safety default).
        if (this.transaction is not null)
        {
            await this.transaction.DisposeAsync();
            this.transaction = null;
        }

        if (this.connection is not null)
        {
            await this.connection.DisposeAsync();
            this.connection = null;
        }
    }
}
