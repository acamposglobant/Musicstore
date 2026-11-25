namespace MusicStore.Infrastructure.Repositories;

using Dapper;
using Microsoft.Extensions.Logging;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using Npgsql;

/// <summary>
/// Dapper-backed persistence for <see cref="Song"/>.
/// <see cref="TimeSpan"/> maps natively to PostgreSQL <c>interval</c> through
/// Npgsql (no custom type handler required — verified by round-trip tests).
/// Connection per operation; parameterized SQL only.
/// </summary>
public sealed class SongRepository(
    NpgsqlDataSource dataSource,
    ILogger<SongRepository> logger) : ISongRepository
{
    public async Task<IReadOnlyList<Song>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var songs = await connection.QueryAsync<Song>(
            "SELECT song_id AS Id, album_id AS AlbumId, name AS Name, duration AS Duration FROM song ORDER BY song_id");
        logger.LogDebug("Repository Song.GetAll | SQL: SELECT song_id, album_id, name, duration FROM song ORDER BY song_id | Rows: {Rows}", songs.AsList().Count);
        return songs.AsList();
    }

    public async Task<IReadOnlyList<Song>> GetByAlbumIdAsync(int albumId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var songs = await connection.QueryAsync<Song>(
            "SELECT song_id AS Id, album_id AS AlbumId, name AS Name, duration AS Duration FROM song WHERE album_id = @albumId ORDER BY song_id",
            new { albumId });
        logger.LogDebug("Repository Song.GetByAlbumId | SQL: SELECT ... WHERE album_id = @albumId | Params: {@Params} | Rows: {Rows}", new { albumId }, songs.AsList().Count);
        return songs.AsList();
    }

    public async Task<Song?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var song = await connection.QuerySingleOrDefaultAsync<Song>(
            "SELECT song_id AS Id, album_id AS AlbumId, name AS Name, duration AS Duration FROM song WHERE song_id = @id",
            new { id });
        logger.LogDebug("Repository Song.GetById | SQL: SELECT ... WHERE song_id = @id | Params: {@Params} | Found: {Found}", new { id }, song is not null);
        return song;
    }

    public async Task<Song> AddAsync(Song song, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleAsync<int>(
            """
            INSERT INTO song (album_id, name, duration)
            VALUES (@albumId, @name, @duration)
            RETURNING song_id
            """,
            new { albumId = song.AlbumId, name = song.Name, duration = song.Duration });
        logger.LogDebug("Repository Song.Add | SQL: INSERT INTO song (album_id, name, duration) RETURNING song_id | Params: {@Params} | ResultId: {ResultId}", new { albumId = song.AlbumId, name = song.Name, duration = song.Duration }, id);
        return new Song(id, song.AlbumId, song.Name, song.Duration);
    }

    public async Task UpdateAsync(Song song, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "UPDATE song SET name = @name, duration = @duration WHERE song_id = @id",
            new { name = song.Name, duration = song.Duration, id = song.Id });
        logger.LogDebug("Repository Song.Update | SQL: UPDATE song SET name, duration WHERE song_id = @id | Params: {@Params} | Rows: {Rows}", new { id = song.Id, name = song.Name, duration = song.Duration }, affected);
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Song {song.Id} not found.");
        }
    }

    public async Task DeleteAsync(Song song, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM song WHERE song_id = @id",
            new { id = song.Id });
        logger.LogDebug("Repository Song.Delete | SQL: DELETE FROM song WHERE song_id = @id | Params: {@Params} | Rows: {Rows}", new { id = song.Id }, affected);
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Song {song.Id} not found.");
        }
    }
}
