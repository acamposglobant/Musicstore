namespace MusicStore.Infrastructure.Repositories;

using Dapper;
using Microsoft.Extensions.Logging;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using Npgsql;

/// <summary>
/// Dapper-backed persistence for <see cref="Album"/>, including the
/// multi-mapping read path that replaces the legacy EDMX navigation-property
/// loads. Connection per operation; parameterized SQL only.
/// </summary>
public sealed class AlbumRepository(
    NpgsqlDataSource dataSource,
    ILogger<AlbumRepository> logger) : IAlbumRepository
{
    public async Task<IReadOnlyList<Album>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var albums = await connection.QueryAsync<Album>(
            "SELECT album_id AS Id, artist_id AS ArtistId, genre_id AS GenreId, title AS Title FROM album ORDER BY album_id");
        logger.LogDebug("Repository Album.GetAll | SQL: SELECT album_id, artist_id, genre_id, title FROM album ORDER BY album_id | Rows: {Rows}", albums.AsList().Count);
        return albums.AsList();
    }

    public async Task<IReadOnlyList<Album>> GetAllWithArtistAndGenreAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var albums = await connection.QueryAsync<Album, Artist, Genre, Album>(
            """
            SELECT a.album_id AS Id, a.artist_id AS ArtistId, a.genre_id AS GenreId, a.title AS Title,
                   ar.artist_id AS Id, ar.name AS Name,
                   g.genre_id AS Id, g.name AS Name, g.description AS Description
            FROM album a
            JOIN artist ar ON ar.artist_id = a.artist_id
            JOIN genre g ON g.genre_id = a.genre_id
            ORDER BY a.album_id
            """,
            (row, artist, genre) => new Album(row.Id, artist, genre, row.Title),
            splitOn: "Id,Id");
        logger.LogDebug("Repository Album.GetAllWithArtistAndGenre | SQL: SELECT ... FROM album JOIN artist JOIN genre | Rows: {Rows}", albums.AsList().Count);
        return albums.AsList();
    }

    public async Task<Album?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var album = await connection.QuerySingleOrDefaultAsync<Album>(
            "SELECT album_id AS Id, artist_id AS ArtistId, genre_id AS GenreId, title AS Title FROM album WHERE album_id = @id",
            new { id });
        logger.LogDebug("Repository Album.GetById | SQL: SELECT ... WHERE album_id = @id | Params: {@Params} | Found: {Found}", new { id }, album is not null);
        return album;
    }

    public async Task<Album> AddAsync(Album album, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleAsync<int>(
            """
            INSERT INTO album (artist_id, genre_id, title)
            VALUES (@artistId, @genreId, @title)
            RETURNING album_id
            """,
            new { artistId = album.ArtistId, genreId = album.GenreId, title = album.Title });
        logger.LogDebug("Repository Album.Add | SQL: INSERT INTO album (artist_id, genre_id, title) RETURNING album_id | Params: {@Params} | ResultId: {ResultId}", new { artistId = album.ArtistId, genreId = album.GenreId, title = album.Title }, id);
        return new Album(id, album.ArtistId, album.GenreId, album.Title);
    }

    public async Task UpdateAsync(Album album, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "UPDATE album SET title = @title WHERE album_id = @id",
            new { title = album.Title, id = album.Id });
        logger.LogDebug("Repository Album.Update | SQL: UPDATE album SET title WHERE album_id = @id | Params: {@Params} | Rows: {Rows}", new { id = album.Id, title = album.Title }, affected);
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Album {album.Id} not found.");
        }
    }

    public async Task DeleteAsync(Album album, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM album WHERE album_id = @id",
            new { id = album.Id });
        logger.LogDebug("Repository Album.Delete | SQL: DELETE FROM album WHERE album_id = @id | Params: {@Params} | Rows: {Rows}", new { id = album.Id }, affected);
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Album {album.Id} not found.");
        }
    }
}
