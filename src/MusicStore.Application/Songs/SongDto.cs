namespace MusicStore.Application.Songs;

/// <summary>Read model for a song. Duration is a true <see cref="TimeSpan"/> (PostgreSQL <c>interval</c>).</summary>
public record SongDto(int Id, int AlbumId, string Name, TimeSpan Duration);
