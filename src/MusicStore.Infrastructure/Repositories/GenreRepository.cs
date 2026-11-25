namespace MusicStore.Infrastructure.Repositories;

using Dapper;
using Microsoft.Extensions.Logging;
using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using Npgsql;

/// <summary>
/// Dapper-backed persistence for <see cref="Genre"/>.
/// Connection per operation (from the pooled <see cref="NpgsqlDataSource"/>) —
/// replaces the legacy shared-<c>DbContext</c> <c>BaseRepository</c> (fixes the
/// <c>GetGenres</c> dispose-bug class). Parameterized SQL only.
/// </summary>
public sealed class GenreRepository(
    NpgsqlDataSource dataSource,
    ILogger<GenreRepository> logger) : IGenreRepository
{
    public async Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var genres = await connection.QueryAsync<Genre>(
            "SELECT genre_id AS Id, name AS Name, description AS Description FROM genre ORDER BY genre_id");
        logger.LogDebug("Repository Genre.GetAll | SQL: SELECT genre_id, name, description FROM genre ORDER BY genre_id | Rows: {Rows}", genres.AsList().Count);
        return genres.AsList();
    }

    public async Task<Genre?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var genre = await connection.QuerySingleOrDefaultAsync<Genre>(
            "SELECT genre_id AS Id, name AS Name, description AS Description FROM genre WHERE genre_id = @id",
            new { id });
        logger.LogDebug("Repository Genre.GetById | SQL: SELECT ... WHERE genre_id = @id | Params: {@Params} | Found: {Found}", new { id }, genre is not null);
        return genre;
    }

    public async Task<Genre> AddAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleAsync<int>(
            """
            INSERT INTO genre (name, description)
            VALUES (@name, @description)
            RETURNING genre_id
            """,
            new { name = genre.Name, description = genre.Description });
        logger.LogDebug("Repository Genre.Add | SQL: INSERT INTO genre (name, description) RETURNING genre_id | Params: {@Params} | ResultId: {ResultId}", new { name = genre.Name, description = genre.Description }, id);
        return new Genre(id, genre.Name, genre.Description);
    }

    public async Task UpdateAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "UPDATE genre SET name = @name, description = @description WHERE genre_id = @id",
            new { name = genre.Name, description = genre.Description, id = genre.Id });
        logger.LogDebug("Repository Genre.Update | SQL: UPDATE genre SET name, description WHERE genre_id = @id | Params: {@Params} | Rows: {Rows}", new { id = genre.Id, name = genre.Name, description = genre.Description }, affected);
        if (affected == 0)
        {
            throw new NotFoundException(nameof(Genre), genre.Id);
        }
    }

    public async Task DeleteAsync(Genre genre, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM genre WHERE genre_id = @id",
            new { id = genre.Id });
        logger.LogDebug("Repository Genre.Delete | SQL: DELETE FROM genre WHERE genre_id = @id | Params: {@Params} | Rows: {Rows}", new { id = genre.Id }, affected);
        if (affected == 0)
        {
            throw new NotFoundException(nameof(Genre), genre.Id);
        }
    }
}
