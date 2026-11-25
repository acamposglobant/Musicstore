namespace MusicStore.Application.Interfaces;

using MusicStore.Domain;

/// <summary>
/// Async CRUD contract for <see cref="Genre"/> persistence.
/// Legacy: <c>MusicStore.Data.IRepositoryGenre</c> (sync EF6, IDisposable).
/// </summary>
public interface IGenreRepository
{
    Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Genre?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Genre> AddAsync(Genre genre, CancellationToken cancellationToken = default);

    Task UpdateAsync(Genre genre, CancellationToken cancellationToken = default);

    Task DeleteAsync(Genre genre, CancellationToken cancellationToken = default);
}
