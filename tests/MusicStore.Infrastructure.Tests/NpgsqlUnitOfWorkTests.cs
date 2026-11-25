namespace MusicStore.Infrastructure.Tests;

using Dapper;
using MusicStore.Infrastructure.UnitOfWork;
using Xunit;

/// <summary>
/// Transaction-boundary tests for <see cref="NpgsqlUnitOfWork"/> against a real
/// PostgreSQL database: commit persists, rollback discards, and disposing
/// without commit rolls back implicitly (the safety default that replaces the
/// legacy EF6 <c>UnitOfWork</c> with its <c>Roolback</c> typo semantics).
/// </summary>
public sealed class NpgsqlUnitOfWorkTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task CommitAsync_persists_changes()
    {
        var dataSource = fixture.DataSource;
        var unitOfWork = new NpgsqlUnitOfWork(dataSource);
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await InsertArtistAsync(unitOfWork.Connection, "Committed Artist");
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, await CountArtistsNamedAsync(dataSource, "Committed Artist"));
    }

    [Fact]
    public async Task RollbackAsync_discards_changes()
    {
        var dataSource = fixture.DataSource;
        var unitOfWork = new NpgsqlUnitOfWork(dataSource);
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await InsertArtistAsync(unitOfWork.Connection, "Rolled Back Artist");
        await unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, await CountArtistsNamedAsync(dataSource, "Rolled Back Artist"));
    }

    [Fact]
    public async Task Dispose_without_commit_rolls_back_implicitly()
    {
        var dataSource = fixture.DataSource;
        var unitOfWork = new NpgsqlUnitOfWork(dataSource);
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await InsertArtistAsync(unitOfWork.Connection, "Abandoned Artist");
        await unitOfWork.DisposeAsync();

        Assert.Equal(0, await CountArtistsNamedAsync(dataSource, "Abandoned Artist"));
    }

    [Fact]
    public async Task BeginTransactionAsync_twice_throws()
    {
        var unitOfWork = new NpgsqlUnitOfWork(fixture.DataSource);
        await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Connection_before_begin_throws()
    {
        await using var unitOfWork = new NpgsqlUnitOfWork(fixture.DataSource);

        Assert.Throws<InvalidOperationException>(() => unitOfWork.Connection);
    }

    [Fact]
    public async Task Commit_without_transaction_is_a_no_op()
    {
        var unitOfWork = new NpgsqlUnitOfWork(fixture.DataSource);

        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);
        await unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);
    }

    private static async Task InsertArtistAsync(System.Data.Common.DbConnection connection, string name)
    {
        await connection.ExecuteAsync(
            "INSERT INTO artist (name) VALUES (@name)",
            new { name });
    }

    private static async Task<int> CountArtistsNamedAsync(Npgsql.NpgsqlDataSource dataSource, string name)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM artist WHERE name = @name",
            new { name });
    }
}
