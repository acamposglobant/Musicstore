namespace MusicStore.Application.Artists;

/// <summary>Payload for creating an artist.</summary>
public record CreateArtistRequest(string Name);

/// <summary>Payload for renaming an artist.</summary>
public record UpdateArtistRequest(string Name);
