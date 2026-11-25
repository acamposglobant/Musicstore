namespace MusicStore.Application.Albums;

using MusicStore.Domain;

internal static class AlbumMappings
{
    public static AlbumDto ToDto(this Album album) => new(album.Id, album.ArtistId, album.GenreId, album.Title);

    public static AlbumDetailsDto ToDetailsDto(this Album album) =>
        new(album.Id, album.ArtistId, album.Artist.Name, album.GenreId, album.Genre.Name, album.Title);
}
