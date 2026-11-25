namespace MusicStore.Application.Genres;

using Microsoft.Extensions.Logging;
using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;

/// <summary>Returns all genres. Legacy: <c>MusicStoreComponent.GetGenres()</c> (the one with the dispose bug).</summary>
public sealed class GetGenresHandler(IGenreRepository genreRepository)
{
    public async Task<IReadOnlyList<GenreDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var genres = await genreRepository.GetAllAsync(cancellationToken);
        return [.. genres.Select(g => g.ToDto())];
    }
}

/// <summary>Returns a single genre or throws <see cref="NotFoundException"/>.</summary>
public sealed class GetGenreByIdHandler(IGenreRepository genreRepository)
{
    public async Task<GenreDto> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var genre = await genreRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Genre), id);
        return genre.ToDto();
    }
}

/// <summary>Creates a genre and returns it with its database-generated id.</summary>
public sealed class CreateGenreHandler(
    IGenreRepository genreRepository,
    ILogger<CreateGenreHandler> logger)
{
    public async Task<GenreDto> HandleAsync(CreateGenreRequest request, CancellationToken cancellationToken = default)
    {
        // MIG-36: one line per handler call with operation + parameters.
        logger.LogInformation("Handler CreateGenre | Name: {Name} | Description: {Description}", request.Name, request.Description);

        var genre = new Genre(request.Name, request.Description);
        var created = await genreRepository.AddAsync(genre, cancellationToken);
        return created.ToDto();
    }
}

/// <summary>Updates an existing genre. Throws <see cref="NotFoundException"/> when the genre does not exist.</summary>
public sealed class UpdateGenreHandler(
    IGenreRepository genreRepository,
    ILogger<UpdateGenreHandler> logger)
{
    public async Task HandleAsync(int id, UpdateGenreRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handler UpdateGenre | Id: {Id} | Name: {Name}", id, request.Name);

        var genre = await genreRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Genre), id);

        genre.UpdateDetails(request.Name, request.Description);
        await genreRepository.UpdateAsync(genre, cancellationToken);
    }
}

/// <summary>Deletes a genre. Throws <see cref="NotFoundException"/> when the genre does not exist.</summary>
public sealed class DeleteGenreHandler(
    IGenreRepository genreRepository,
    ILogger<DeleteGenreHandler> logger)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handler DeleteGenre | Id: {Id}", id);

        var genre = await genreRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Genre), id);

        await genreRepository.DeleteAsync(genre, cancellationToken);
    }
}
