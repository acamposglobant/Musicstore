namespace MusicStore.Application.Songs;

using MusicStore.Domain;

internal static class SongMappings
{
    public static SongDto ToDto(this Song song) => new(song.Id, song.AlbumId, song.Name, song.Duration);
}
