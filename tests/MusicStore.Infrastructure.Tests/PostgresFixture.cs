namespace MusicStore.Infrastructure.Tests;

using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Real PostgreSQL container shared by all tests in the assembly (one container,
/// many databases): each test class gets its own database created from the
/// production DDL + seed scripts in <c>db/</c>, so tests are isolated from each
/// other while paying container startup only once.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private NpgsqlDataSource dataSource = null!;

    public async ValueTask InitializeAsync()
    {
        await this.container.StartAsync();
        this.dataSource = NpgsqlDataSource.Create(this.container.GetConnectionString());

        // Apply the production schema + seed scripts (same files DbUp runs in the
        // AppHost), in filename order — the tests exercise the real DDL.
        await ApplyScriptsAsync(this.dataSource);
    }

    public async ValueTask DisposeAsync()
    {
        await this.dataSource.DisposeAsync();
        await this.container.DisposeAsync();
    }

    /// <summary>Shared data source for the default database (schema + seed applied).</summary>
    public NpgsqlDataSource DataSource => this.dataSource;

    private static async Task ApplyScriptsAsync(NpgsqlDataSource targetDataSource)
    {
        // 001 (DDL) + 100 (seed) define the schema and baseline data the repositories
        // are tested against; 200 is a read-only parity check and 300 provisions the
        // Songs microservice role — neither is part of the repository contract.
        var dbDirectory = FindDbDirectory();
        foreach (var scriptName in new[] { "001_create_tables.sql", "100_seed.sql" })
        {
            await using var connection = await targetDataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(await File.ReadAllTextAsync(Path.Combine(dbDirectory, scriptName)));
        }
    }

    private static string FindDbDirectory()
    {
        // Walk up from the test bin folder to the repo root (marker: MusicStore.slnx).
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MusicStore.slnx")))
        {
            directory = directory.Parent!;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the repository root (MusicStore.slnx).");
        }

        return Path.Combine(directory.FullName, "db");
    }
}
