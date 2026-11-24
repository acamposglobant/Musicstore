namespace MusicStore.Infrastructure.Repositories;

using Dapper;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using Npgsql;

/// <summary>
/// Dapper-backed persistence for <see cref="Artist"/>.
/// Connection per operation (from the pooled <see cref="NpgsqlDataSource"/>) —
/// replaces the legacy shared-<c>DbContext</c> <c>BaseRepository</c> (fixes the
/// <c>GetGenres</c> dispose-bug class). Parameterized SQL only.
/// </summary>
public sealed class ArtistRepository : IArtistRepository
{
    private readonly NpgsqlDataSource dataSource;

    public ArtistRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var artists = await connection.QueryAsync<Artist>(
            "SELECT artist_id AS Id, name AS Name FROM artist ORDER BY artist_id");
        return artists.AsList();
    }

    public async Task<Artist?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Artist>(
            "SELECT artist_id AS Id, name AS Name FROM artist WHERE artist_id = @id",
            new { id });
    }

    public async Task<Artist> AddAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleAsync<int>(
            """
            INSERT INTO artist (name)
            VALUES (@name)
            RETURNING artist_id
            """,
            new { name = artist.Name });
        return new Artist(id, artist.Name);
    }

    public async Task UpdateAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "UPDATE artist SET name = @name WHERE artist_id = @id",
            new { name = artist.Name, id = artist.Id });
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Artist {artist.Id} not found.");
        }
    }

    public async Task DeleteAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        await using var connection = await this.dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM artist WHERE artist_id = @id",
            new { id = artist.Id });
        if (affected == 0)
        {
            throw new KeyNotFoundException($"Artist {artist.Id} not found.");
        }
    }
}
