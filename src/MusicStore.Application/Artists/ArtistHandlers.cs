namespace MusicStore.Application.Artists;

using Microsoft.Extensions.Logging;
using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;

/// <summary>
/// Returns all artists. Legacy: <c>MusicStoreComponent.GetArtists()</c>.
/// </summary>
public sealed class GetArtistsHandler(IArtistRepository artistRepository)
{
    public async Task<IReadOnlyList<ArtistDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var artists = await artistRepository.GetAllAsync(cancellationToken);
        return [.. artists.Select(a => a.ToDto())];
    }
}

/// <summary>
/// Returns a single artist or throws <see cref="NotFoundException"/>. Legacy: <c>GetArtist()</c> SOAP op.
/// </summary>
public sealed class GetArtistByIdHandler(IArtistRepository artistRepository)
{
    public async Task<ArtistDto> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var artist = await artistRepository.GetByIdAsync(id, cancellationToken)
                     ?? throw new NotFoundException(nameof(Artist), id);
        return artist.ToDto();
    }
}

/// <summary>
/// Creates an artist and returns it with its database-generated id.
/// </summary>
public sealed class CreateArtistHandler(
    IArtistRepository artistRepository,
    ILogger<CreateArtistHandler> logger)
{
    public async Task<ArtistDto> HandleAsync(CreateArtistRequest request, CancellationToken cancellationToken = default)
    {
        // Validate here (not the domain guard) so invalid input maps to 422
        // ProblemDetails instead of an unexpected 500 (MIG-17 contract).
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Artist name is required.");
        }

        // MIG-36: one line per handler call with operation + parameters; the
        // CorrelationId property flows from the API middleware's LogContext.
        logger.LogInformation("Handler CreateArtist | Name: {Name}", request.Name);

        var artist = new Artist(request.Name);
        var created = await artistRepository.AddAsync(artist, cancellationToken);
        return created.ToDto();
    }
}

/// <summary>
/// Renames an existing artist. Throws <see cref="NotFoundException"/> when the artist does not exist.
/// </summary>
public sealed class UpdateArtistHandler(
    IArtistRepository artistRepository,
    ILogger<UpdateArtistHandler> logger)
{
    public async Task HandleAsync(int id, UpdateArtistRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handler UpdateArtist | Id: {Id} | Name: {Name}", id, request.Name);

        var artist = await artistRepository.GetByIdAsync(id, cancellationToken)
                     ?? throw new NotFoundException(nameof(Artist), id);

        artist.Rename(request.Name);
        await artistRepository.UpdateAsync(artist, cancellationToken);
    }
}

/// <summary>
/// Deletes an artist (cascades to its albums/songs per schema FK rules).
/// Throws <see cref="NotFoundException"/> when the artist does not exist.
/// </summary>
public sealed class DeleteArtistHandler(
    IArtistRepository artistRepository,
    ILogger<DeleteArtistHandler> logger)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handler DeleteArtist | Id: {Id}", id);

        var artist = await artistRepository.GetByIdAsync(id, cancellationToken)
                     ?? throw new NotFoundException(nameof(Artist), id);

        await artistRepository.DeleteAsync(artist, cancellationToken);
    }
}
