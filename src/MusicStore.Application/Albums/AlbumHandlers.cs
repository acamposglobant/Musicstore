namespace MusicStore.Application.Albums;

using Microsoft.Extensions.Logging;
using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;

/// <summary>Returns all albums (flat). Legacy: <c>RepositoryAlbum.GetAll()</c> via the component.</summary>
public sealed class GetAlbumsHandler(IAlbumRepository albumRepository)
{
    public async Task<IReadOnlyList<AlbumDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var albums = await albumRepository.GetAllAsync(cancellationToken);
        return [.. albums.Select(a => a.ToDto())];
    }
}

/// <summary>
/// Returns all albums with artist and genre names populated (Dapper multi-mapping read path).
/// Legacy: EDMX navigation properties.
/// </summary>
public sealed class GetAlbumsWithDetailsHandler(IAlbumRepository albumRepository)
{
    public async Task<IReadOnlyList<AlbumDetailsDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var albums = await albumRepository.GetAllWithArtistAndGenreAsync(cancellationToken);
        return [.. albums.Select(a => a.ToDetailsDto())];
    }
}

/// <summary>Returns a single album or throws <see cref="NotFoundException"/>.</summary>
public sealed class GetAlbumByIdHandler(IAlbumRepository albumRepository)
{
    public async Task<AlbumDto> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var album = await albumRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Album), id);
        return album.ToDto();
    }
}

/// <summary>
/// Creates an album. Validates that the referenced artist and genre exist
/// (legacy relied on SQL FK errors surfacing as <c>ExceptionReturn</c>).
/// </summary>
public sealed class CreateAlbumHandler(
    IAlbumRepository albumRepository,
    IArtistRepository artistRepository,
    IGenreRepository genreRepository,
    ILogger<CreateAlbumHandler> logger)
{
    public async Task<AlbumDto> HandleAsync(CreateAlbumRequest request, CancellationToken cancellationToken = default)
    {
        // MIG-36: one line per handler call with operation + parameters.
        logger.LogInformation("Handler CreateAlbum | ArtistId: {ArtistId} | GenreId: {GenreId} | Title: {Title}", request.ArtistId, request.GenreId, request.Title);

        _ = await artistRepository.GetByIdAsync(request.ArtistId, cancellationToken)
            ?? throw new ValidationException($"Artist {request.ArtistId} does not exist.");

        _ = await genreRepository.GetByIdAsync(request.GenreId, cancellationToken)
            ?? throw new ValidationException($"Genre {request.GenreId} does not exist.");

        var album = new Album(request.ArtistId, request.GenreId, request.Title);
        var created = await albumRepository.AddAsync(album, cancellationToken);
        return created.ToDto();
    }
}

/// <summary>Updates an existing album. Throws <see cref="NotFoundException"/> when the album does not exist.</summary>
public sealed class UpdateAlbumHandler(
    IAlbumRepository albumRepository,
    IArtistRepository artistRepository,
    IGenreRepository genreRepository,
    ILogger<UpdateAlbumHandler> logger)
{
    public async Task HandleAsync(int id, UpdateAlbumRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handler UpdateAlbum | Id: {Id} | ArtistId: {ArtistId} | GenreId: {GenreId} | Title: {Title}", id, request.ArtistId, request.GenreId, request.Title);

        var album = await albumRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Album), id);

        _ = await artistRepository.GetByIdAsync(request.ArtistId, cancellationToken)
            ?? throw new ValidationException($"Artist {request.ArtistId} does not exist.");

        _ = await genreRepository.GetByIdAsync(request.GenreId, cancellationToken)
            ?? throw new ValidationException($"Genre {request.GenreId} does not exist.");

        album.UpdateTitle(request.Title);
        await albumRepository.UpdateAsync(album, cancellationToken);
    }
}

/// <summary>Deletes an album (cascades to its songs). Throws <see cref="NotFoundException"/> when the album does not exist.</summary>
public sealed class DeleteAlbumHandler(
    IAlbumRepository albumRepository,
    ILogger<DeleteAlbumHandler> logger)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handler DeleteAlbum | Id: {Id}", id);

        var album = await albumRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Album), id);

        await albumRepository.DeleteAsync(album, cancellationToken);
    }
}
