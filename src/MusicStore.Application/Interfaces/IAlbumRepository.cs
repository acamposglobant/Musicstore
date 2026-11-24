namespace MusicStore.Application.Interfaces;

using MusicStore.Domain;

/// <summary>
/// Async CRUD contract for <see cref="Album"/> persistence.
/// Legacy: <c>MusicStore.Data.IRepositoryAlbum</c> (sync EF6, IDisposable).
/// </summary>
public interface IAlbumRepository
{
    Task<IReadOnlyList<Album>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads albums with their <see cref="Album.Artist"/> and <see cref="Album.Genre"/> populated
    /// (replaces the legacy EDMX navigation-property load; implemented via Dapper multi-mapping).
    /// </summary>
    Task<IReadOnlyList<Album>> GetAllWithArtistAndGenreAsync(CancellationToken cancellationToken = default);

    Task<Album?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Album> AddAsync(Album album, CancellationToken cancellationToken = default);

    Task UpdateAsync(Album album, CancellationToken cancellationToken = default);

    Task DeleteAsync(Album album, CancellationToken cancellationToken = default);
}
