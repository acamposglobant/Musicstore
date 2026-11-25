namespace MusicStore.Application.Songs;

/// <summary>Payload for creating a song.</summary>
public record CreateSongRequest(int AlbumId, string Name, TimeSpan Duration);

/// <summary>Payload for updating a song.</summary>
public record UpdateSongRequest(string Name, TimeSpan Duration);
