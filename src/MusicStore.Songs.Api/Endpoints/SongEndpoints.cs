namespace MusicStore.Api.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MusicStore.Application.Songs;

/// <summary>
/// Songs CRUD endpoints (owned by the Songs microservice, MIG-21).
/// Duration is a true <see cref="TimeSpan"/> serialized as ISO-8601 duration
/// (PostgreSQL <c>interval</c> round-trip). Legacy: EDMX <c>Album.Songs</c> navigation.
/// </summary>
public static class SongEndpoints
{
    public static IEndpointRouteBuilder MapSongEndpoints(this IEndpointRouteBuilder app)
    {
        var songs = app.MapGroup("/songs")
            .WithTags("Songs");

        _ = songs.MapGet("/", async (GetSongsHandler getAllHandler, GetSongsByAlbumHandler getByAlbumHandler, int? albumId, CancellationToken cancellationToken) =>
            {
                // albumId filter replaces the catalog's GET /albums/{id}/songs route
                // (the catalog API no longer serves songs after the MIG-21 split).
                var songs = albumId.HasValue
                    ? await getByAlbumHandler.HandleAsync(albumId.Value, cancellationToken)
                    : await getAllHandler.HandleAsync(cancellationToken);
                return TypedResults.Ok(songs);
            })
            .WithName("GetSongs")
            .WithSummary("Gets all songs, optionally filtered by album")
            .Produces<IReadOnlyList<SongDto>>(StatusCodes.Status200OK);

        _ = songs.MapGet("/{id:int}", async (int id, GetSongByIdHandler handler, CancellationToken cancellationToken) =>
            {
                var song = await handler.HandleAsync(id, cancellationToken);
                return TypedResults.Ok(song);
            })
            .WithName("GetSongById")
            .WithSummary("Gets a song by id")
            .Produces<SongDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = songs.MapPost("/", async (CreateSongRequest request, CreateSongHandler handler, CancellationToken cancellationToken) =>
            {
                var created = await handler.HandleAsync(request, cancellationToken);
                return TypedResults.Created($"/songs/{created.Id}", created);
            })
            .WithName("CreateSong")
            .WithSummary("Creates a song on an existing album")
            .Produces<SongDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = songs.MapPut("/{id:int}", async (int id, UpdateSongRequest request, UpdateSongHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, request, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("UpdateSong")
            .WithSummary("Updates a song")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = songs.MapDelete("/{id:int}", async (int id, DeleteSongHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("DeleteSong")
            .WithSummary("Deletes a song")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
