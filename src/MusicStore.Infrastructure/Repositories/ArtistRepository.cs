namespace MusicStore.Infrastructure.Repositories;

using Dapper;
using Microsoft.Extensions.Logging;
using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using Npgsql;

/// <summary>
/// Dapper-backed persistence for <see cref="Artist"/>.
/// Connection per operation (from the pooled <see cref="NpgsqlDataSource"/>) —
/// replaces the legacy shared-<c>DbContext</c> <c>BaseRepository</c> (fixes the
/// <c>GetGenres</c> dispose-bug class). Parameterized SQL only.
/// </summary>
public sealed class ArtistRepository(
    NpgsqlDataSource dataSource,
    ILogger<ArtistRepository> logger) : IArtistRepository
{
    public async Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var artists = await connection.QueryAsync<Artist>(
            "SELECT artist_id AS Id, name AS Name FROM artist ORDER BY artist_id");
        logger.LogDebug("Repository Artist.GetAll | SQL: {Sql} | Rows: {Rows}", "SELECT artist_id, name FROM artist ORDER BY artist_id", artists.AsList().Count);
        return artists.AsList();
    }

    public async Task<Artist?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var artist = await connection.QuerySingleOrDefaultAsync<Artist>(
            "SELECT artist_id AS Id, name AS Name FROM artist WHERE artist_id = @id",
            new { id });
        logger.LogDebug("Repository Artist.GetById | SQL: SELECT ... WHERE artist_id = @id | Params: {@Params} | Found: {Found}", new { id }, artist is not null);
        return artist;
    }

    public async Task<Artist> AddAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleAsync<int>(
            """
            INSERT INTO artist (name)
            VALUES (@name)
            RETURNING artist_id
            """,
            new { name = artist.Name });
        logger.LogDebug("Repository Artist.Add | SQL: INSERT INTO artist (name) RETURNING artist_id | Params: {@Params} | ResultId: {ResultId}", new { name = artist.Name }, id);
        return new Artist(id, artist.Name);
    }

    public async Task UpdateAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "UPDATE artist SET name = @name WHERE artist_id = @id",
            new { name = artist.Name, id = artist.Id });
        logger.LogDebug("Repository Artist.Update | SQL: UPDATE artist SET name = @name WHERE artist_id = @id | Params: {@Params} | Rows: {Rows}", new { id = artist.Id, name = artist.Name }, affected);
        if (affected == 0)
        {
            throw new NotFoundException(nameof(Artist), artist.Id);
        }
    }

    public async Task DeleteAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM artist WHERE artist_id = @id",
            new { id = artist.Id });
        logger.LogDebug("Repository Artist.Delete | SQL: DELETE FROM artist WHERE artist_id = @id | Params: {@Params} | Rows: {Rows}", new { id = artist.Id }, affected);
        if (affected == 0)
        {
            throw new NotFoundException(nameof(Artist), artist.Id);
        }
    }
}
