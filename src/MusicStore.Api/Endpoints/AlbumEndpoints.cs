namespace MusicStore.Api.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MusicStore.Application.Albums;
using MusicStore.Application.Songs;

/// <summary>
/// Albums CRUD endpoints, including the enriched read path
/// (<c>GET /albums?include=artist,genre</c> — Dapper multi-mapping) and the
/// nested songs collection. Legacy: <c>GetArtistResponse</c> SOAP payload.
/// </summary>
public static class AlbumEndpoints
{
    public static IEndpointRouteBuilder MapAlbumEndpoints(this IEndpointRouteBuilder app)
    {
        var albums = app.MapGroup("/albums")
            .WithTags("Albums");

        _ = albums.MapGet("/", async (GetAlbumsHandler handler, CancellationToken cancellationToken) =>
            {
                var albums = await handler.HandleAsync(cancellationToken);
                return TypedResults.Ok(albums);
            })
            .WithName("GetAlbums")
            .WithSummary("Gets all albums (flat)")
            .Produces<IReadOnlyList<AlbumDto>>(StatusCodes.Status200OK);

        _ = albums.MapGet("/with-details", async (GetAlbumsWithDetailsHandler handler, CancellationToken cancellationToken) =>
            {
                var albums = await handler.HandleAsync(cancellationToken);
                return TypedResults.Ok(albums);
            })
            .WithName("GetAlbumsWithDetails")
            .WithSummary("Gets all albums with artist and genre names (multi-mapping read path)")
            .Produces<IReadOnlyList<AlbumDetailsDto>>(StatusCodes.Status200OK);

        _ = albums.MapGet("/{id:int}", async (int id, GetAlbumByIdHandler handler, CancellationToken cancellationToken) =>
            {
                var album = await handler.HandleAsync(id, cancellationToken);
                return TypedResults.Ok(album);
            })
            .WithName("GetAlbumById")
            .WithSummary("Gets an album by id")
            .Produces<AlbumDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = albums.MapGet("/{id:int}/songs", async (int id, GetSongsByAlbumHandler handler, CancellationToken cancellationToken) =>
            {
                var songs = await handler.HandleAsync(id, cancellationToken);
                return TypedResults.Ok(songs);
            })
            .WithName("GetAlbumSongs")
            .WithSummary("Gets all songs of an album")
            .Produces<IReadOnlyList<SongDto>>(StatusCodes.Status200OK);

        _ = albums.MapPost("/", async (CreateAlbumRequest request, CreateAlbumHandler handler, CancellationToken cancellationToken) =>
            {
                var created = await handler.HandleAsync(request, cancellationToken);
                return TypedResults.Created($"/albums/{created.Id}", created);
            })
            .WithName("CreateAlbum")
            .WithSummary("Creates an album")
            .Produces<AlbumDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = albums.MapPut("/{id:int}", async (int id, UpdateAlbumRequest request, UpdateAlbumHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, request, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("UpdateAlbum")
            .WithSummary("Updates an album")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = albums.MapDelete("/{id:int}", async (int id, DeleteAlbumHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("DeleteAlbum")
            .WithSummary("Deletes an album (cascades to its songs)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
