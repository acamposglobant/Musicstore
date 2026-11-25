namespace MusicStore.Application.Interfaces;

using MusicStore.Domain;

/// <summary>
/// Async CRUD contract for <see cref="Artist"/> persistence.
/// Legacy: <c>MusicStore.Data.IRepositoryArtist</c> (sync EF6, IDisposable).
/// </summary>
public interface IArtistRepository
{
    Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Artist?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Artist> AddAsync(Artist artist, CancellationToken cancellationToken = default);

    Task UpdateAsync(Artist artist, CancellationToken cancellationToken = default);

    Task DeleteAsync(Artist artist, CancellationToken cancellationToken = default);
}
