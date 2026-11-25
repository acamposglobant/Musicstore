namespace MusicStore.Application.Artists;

/// <summary>
/// Read model for an artist. Replaces the legacy <c>GetArtistResponse</c> DataContract
/// (whose <c>responseData</c> was an untyped <c>object</c>).
/// </summary>
public record ArtistDto(int Id, string Name);
