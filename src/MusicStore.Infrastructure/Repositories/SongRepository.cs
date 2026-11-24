namespace MusicStore.Infrastructure.Repositories;

using Dapper;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using Npgsql;

/// <summary>
/// Dapper-backed persistence for <see cref="Song"/>.
/// <see cref="TimeSpan"/> maps natively to PostgreSQL <c>interval</c> through
/// Npgsql (no custom type handler required — verified by round-trip tests).
/// Connection per operation; parameterized SQL only.
/// </summary>
public sealed class SongRepository : ISongRepository
{
    private readonly NpgsqlDataSource dataSource;

    public SongRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<Song>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var songs = await connection.QueryAsync<Song>(
            "SELECT song_id AS Id, album_id AS AlbumId, name AS Name, duration AS Duration FROM song ORDER BY song_id");
        return songs.AsList();
    }

    public async Task<IReadOnlyList<Song>> GetByAlbumIdAsync(int albumId, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var songs = await connection.QueryAsync<Song>(
            "SELECT song_id AS Id, album_id AS AlbumId, name AS Name, duration AS Duration FROM song WHERE album_id = @albumId ORDER BY song_id",
            new { albumId });
        return songs.AsList();
    }

    public async Task<Song?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Song>(
            "SELECT song_id AS Id, album_id AS AlbumId, name AS Name, duration AS Duration FROM song WHERE song_id = @id",
            new { id });
    }

    public async Task<Song> AddAsync(Song song, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleAsync<int>(
            """
            INSERT INTO song (album_id, name, duration)
            VALUES (@albumId, @name, @duration)
            RETURNING song_id
            """,
            new { albumId = song.AlbumId, name = song.Name, duration = song.Duration });
        return new Song(id, song.AlbumId, song.Name, song.Duration);
    }

    public async Task UpdateAsync(Song song, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "UPDATE song SET name = @name, duration = @duration WHERE song_id = @id",
            new { name = song.Name, duration = song.Duration, id = song.Id });
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Song {song.Id} not found.");
        }
    }

    public async Task DeleteAsync(Song song, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM song WHERE song_id = @id",
            new { id = song.Id });
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Song {song.Id} not found.");
        }
    }
}
