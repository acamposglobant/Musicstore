namespace MusicStore.Infrastructure.Repositories;

using Dapper;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using Npgsql;

/// <summary>
/// Dapper-backed persistence for <see cref="Genre"/>.
/// Connection per operation (from the pooled <see cref="NpgsqlDataSource"/>) —
/// replaces the legacy shared-<c>DbContext</c> <c>BaseRepository</c> (fixes the
/// <c>GetGenres</c> dispose-bug class). Parameterized SQL only.
/// </summary>
public sealed class GenreRepository : IGenreRepository
{
    private readonly NpgsqlDataSource dataSource;

    public GenreRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var genres = await connection.QueryAsync<Genre>(
            "SELECT genre_id AS Id, name AS Name, description AS Description FROM genre ORDER BY genre_id");
        return genres.AsList();
    }

    public async Task<Genre?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Genre>(
            "SELECT genre_id AS Id, name AS Name, description AS Description FROM genre WHERE genre_id = @id",
            new { id });
    }

    public async Task<Genre> AddAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleAsync<int>(
            """
            INSERT INTO genre (name, description)
            VALUES (@name, @description)
            RETURNING genre_id
            """,
            new { name = genre.Name, description = genre.Description });
        return new Genre(id, genre.Name, genre.Description);
    }

    public async Task UpdateAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "UPDATE genre SET name = @name, description = @description WHERE genre_id = @id",
            new { name = genre.Name, description = genre.Description, id = genre.Id });
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Genre {genre.Id} not found.");
        }
    }

    public async Task DeleteAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM genre WHERE genre_id = @id",
            new { id = genre.Id });
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Genre {genre.Id} not found.");
        }
    }
}
