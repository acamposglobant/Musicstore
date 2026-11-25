namespace MusicStore.Application.Artists;

using MusicStore.Domain;

internal static class ArtistMappings
{
    public static ArtistDto ToDto(this Artist artist) => new(artist.Id, artist.Name);
}
