namespace MusicStore.Application.Songs;

using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;

/// <summary>Returns all songs.</summary>
public sealed class GetSongsHandler(ISongRepository songRepository)
{
    public async Task<IReadOnlyList<SongDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var songs = await songRepository.GetAllAsync(cancellationToken);
        return [.. songs.Select(s => s.ToDto())];
    }
}

/// <summary>Returns all songs of an album. Legacy: EDMX <c>Album.Songs</c> navigation property.</summary>
public sealed class GetSongsByAlbumHandler(ISongRepository songRepository)
{
    public async Task<IReadOnlyList<SongDto>> HandleAsync(int albumId, CancellationToken cancellationToken = default)
    {
        var songs = await songRepository.GetByAlbumIdAsync(albumId, cancellationToken);
        return [.. songs.Select(s => s.ToDto())];
    }
}

/// <summary>Returns a single song or throws <see cref="NotFoundException"/>.</summary>
public sealed class GetSongByIdHandler(ISongRepository songRepository)
{
    public async Task<SongDto> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var song = await songRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(Song), id);
        return song.ToDto();
    }
}

/// <summary>
/// Creates a song on an existing album. Validates that the album exists
/// (legacy relied on SQL FK errors surfacing as <c>ExceptionReturn</c>).
/// </summary>
public sealed class CreateSongHandler(
    ISongRepository songRepository,
    IAlbumRepository albumRepository)
{
    public async Task<SongDto> HandleAsync(CreateSongRequest request, CancellationToken cancellationToken = default)
    {
        // Validate here (not the domain guard) so invalid input maps to 422
        // ProblemDetails instead of an unexpected 500 (MIG-17 contract).
        if (request.Duration <= TimeSpan.Zero)
        {
            throw new ValidationException("Song duration must be positive.");
        }

        _ = await albumRepository.GetByIdAsync(request.AlbumId, cancellationToken)
            ?? throw new ValidationException($"Album {request.AlbumId} does not exist.");

        var song = new Song(request.AlbumId, request.Name, request.Duration);
        var created = await songRepository.AddAsync(song, cancellationToken);
        return created.ToDto();
    }
}

/// <summary>Updates an existing song. Throws <see cref="NotFoundException"/> when the song does not exist.</summary>
public sealed class UpdateSongHandler(ISongRepository songRepository)
{
    public async Task HandleAsync(int id, UpdateSongRequest request, CancellationToken cancellationToken = default)
    {
        var song = await songRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(Song), id);

        song.UpdateDetails(request.Name, request.Duration);
        await songRepository.UpdateAsync(song, cancellationToken);
    }
}

/// <summary>Deletes a song. Throws <see cref="NotFoundException"/> when the song does not exist.</summary>
public sealed class DeleteSongHandler(ISongRepository songRepository)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var song = await songRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(Song), id);

        await songRepository.DeleteAsync(song, cancellationToken);
    }
}
