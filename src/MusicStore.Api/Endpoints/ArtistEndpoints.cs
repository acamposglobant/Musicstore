namespace MusicStore.Api.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MusicStore.Application.Artists;

/// <summary>
/// Artists CRUD endpoints. Replaces the legacy <c>GetArtist()</c> SOAP operation
/// (<c>IMusicStoreService</c>) and the <c>ServiceReturn</c> envelope.
/// Failures surface as RFC 9457 ProblemDetails via the global exception handler
/// (<c>NotFoundException</c> → 404, <c>ValidationException</c> → 422).
/// </summary>
public static class ArtistEndpoints
{
    public static IEndpointRouteBuilder MapArtistEndpoints(this IEndpointRouteBuilder app)
    {
        var artists = app.MapGroup("/artists")
            .WithTags("Artists");

        _ = artists.MapGet("/", async (GetArtistsHandler handler, CancellationToken cancellationToken) =>
            {
                var artists = await handler.HandleAsync(cancellationToken);
                return TypedResults.Ok(artists);
            })
            .WithName("GetArtists")
            .WithSummary("Gets all artists")
            .Produces<IReadOnlyList<ArtistDto>>(StatusCodes.Status200OK);

        _ = artists.MapGet("/{id:int}", async (int id, GetArtistByIdHandler handler, CancellationToken cancellationToken) =>
            {
                var artist = await handler.HandleAsync(id, cancellationToken);
                return TypedResults.Ok(artist);
            })
            .WithName("GetArtistById")
            .WithSummary("Gets an artist by id")
            .Produces<ArtistDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = artists.MapPost("/", async (CreateArtistRequest request, CreateArtistHandler handler, CancellationToken cancellationToken) =>
            {
                var created = await handler.HandleAsync(request, cancellationToken);
                return TypedResults.Created($"/artists/{created.Id}", created);
            })
            .WithName("CreateArtist")
            .WithSummary("Creates an artist")
            .Produces<ArtistDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = artists.MapPut("/{id:int}", async (int id, UpdateArtistRequest request, UpdateArtistHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, request, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("UpdateArtist")
            .WithSummary("Renames an artist")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = artists.MapDelete("/{id:int}", async (int id, DeleteArtistHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("DeleteArtist")
            .WithSummary("Deletes an artist (cascades to its albums and songs)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
