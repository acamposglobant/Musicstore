namespace MusicStore.Application.Interfaces;

using MusicStore.Domain;

/// <summary>
/// Async CRUD contract for <see cref="Song"/> persistence.
/// Legacy: <c>MusicStore.Data.IRepositorySong</c> (sync EF6, IDisposable).
/// </summary>
public interface ISongRepository
{
    Task<IReadOnlyList<Song>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads all songs belonging to the given album (legacy: EDMX navigation property).</summary>
    Task<IReadOnlyList<Song>> GetByAlbumIdAsync(int albumId, CancellationToken cancellationToken = default);

    Task<Song?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Song> AddAsync(Song song, CancellationToken cancellationToken = default);

    Task UpdateAsync(Song song, CancellationToken cancellationToken = default);

    Task DeleteAsync(Song song, CancellationToken cancellationToken = default);
}
