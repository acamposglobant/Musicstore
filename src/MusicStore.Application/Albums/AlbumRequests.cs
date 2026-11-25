namespace MusicStore.Application.Albums;

/// <summary>Payload for creating an album.</summary>
public record CreateAlbumRequest(int ArtistId, int GenreId, string Title);

/// <summary>Payload for updating an album.</summary>
public record UpdateAlbumRequest(int ArtistId, int GenreId, string Title);
