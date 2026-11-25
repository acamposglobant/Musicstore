namespace MusicStore.Application;

using Microsoft.Extensions.DependencyInjection;
using MusicStore.Application.Albums;
using MusicStore.Application.Artists;
using MusicStore.Application.Genres;
using MusicStore.Application.Songs;

/// <summary>
/// Registers the Application-layer vertical-slice handlers (MIG-16).
/// Handlers are stateless; repositories are injected per request scope.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMusicStoreApplication(this IServiceCollection services)
    {
        // Artists
        services.AddScoped<GetArtistsHandler>();
        services.AddScoped<GetArtistByIdHandler>();
        services.AddScoped<CreateArtistHandler>();
        services.AddScoped<UpdateArtistHandler>();
        services.AddScoped<DeleteArtistHandler>();

        // Genres
        services.AddScoped<GetGenresHandler>();
        services.AddScoped<GetGenreByIdHandler>();
        services.AddScoped<CreateGenreHandler>();
        services.AddScoped<UpdateGenreHandler>();
        services.AddScoped<DeleteGenreHandler>();

        // Albums
        services.AddScoped<GetAlbumsHandler>();
        services.AddScoped<GetAlbumsWithDetailsHandler>();
        services.AddScoped<GetAlbumByIdHandler>();
        services.AddScoped<CreateAlbumHandler>();
        services.AddScoped<UpdateAlbumHandler>();
        services.AddScoped<DeleteAlbumHandler>();

        // Songs
        services.AddScoped<GetSongsHandler>();
        services.AddScoped<GetSongsByAlbumHandler>();
        services.AddScoped<GetSongByIdHandler>();
        services.AddScoped<CreateSongHandler>();
        services.AddScoped<UpdateSongHandler>();
        services.AddScoped<DeleteSongHandler>();

        return services;
    }
}
