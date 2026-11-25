namespace MusicStore.Application.Genres;

/// <summary>Payload for creating a genre.</summary>
public record CreateGenreRequest(string Name, string Description);

/// <summary>Payload for updating a genre.</summary>
public record UpdateGenreRequest(string Name, string Description);
