namespace MusicStore.Application.Genres;

using MusicStore.Domain;

internal static class GenreMappings
{
    public static GenreDto ToDto(this Genre genre) => new(genre.Id, genre.Name, genre.Description);
}
