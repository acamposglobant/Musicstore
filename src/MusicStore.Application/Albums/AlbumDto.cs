namespace MusicStore.Application.Albums;

/// <summary>
/// Read model for an album. Artist/genre are flattened into ids; the enriched
/// read path (<c>WithDetails</c>) carries the related names.
/// </summary>
public record AlbumDto(int Id, int ArtistId, int GenreId, string Title);

/// <summary>Album read model with artist and genre names populated (multi-mapping read path).</summary>
public record AlbumDetailsDto(int Id, int ArtistId, string ArtistName, int GenreId, string GenreName, string Title);
