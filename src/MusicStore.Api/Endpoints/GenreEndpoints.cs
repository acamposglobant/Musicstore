namespace MusicStore.Api.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MusicStore.Application.Genres;

/// <summary>
/// Genres CRUD endpoints. Replaces the legacy <c>GetGenres()</c> SOAP operation
/// (the one with the shared-DbContext dispose bug — connection-per-operation here).
/// Failures surface as RFC 9457 ProblemDetails via the global exception handler.
/// </summary>
public static class GenreEndpoints
{
    public static IEndpointRouteBuilder MapGenreEndpoints(this IEndpointRouteBuilder app)
    {
        var genres = app.MapGroup("/genres")
            .WithTags("Genres");

        _ = genres.MapGet("/", async (GetGenresHandler handler, CancellationToken cancellationToken) =>
            {
                var genres = await handler.HandleAsync(cancellationToken);
                return TypedResults.Ok(genres);
            })
            .WithName("GetGenres")
            .WithSummary("Gets all genres")
            .Produces<IReadOnlyList<GenreDto>>(StatusCodes.Status200OK);

        _ = genres.MapGet("/{id:int}", async (int id, GetGenreByIdHandler handler, CancellationToken cancellationToken) =>
            {
                var genre = await handler.HandleAsync(id, cancellationToken);
                return TypedResults.Ok(genre);
            })
            .WithName("GetGenreById")
            .WithSummary("Gets a genre by id")
            .Produces<GenreDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = genres.MapPost("/", async (CreateGenreRequest request, CreateGenreHandler handler, CancellationToken cancellationToken) =>
            {
                var created = await handler.HandleAsync(request, cancellationToken);
                return TypedResults.Created($"/genres/{created.Id}", created);
            })
            .WithName("CreateGenre")
            .WithSummary("Creates a genre")
            .Produces<GenreDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = genres.MapPut("/{id:int}", async (int id, UpdateGenreRequest request, UpdateGenreHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, request, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("UpdateGenre")
            .WithSummary("Updates a genre")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        _ = genres.MapDelete("/{id:int}", async (int id, DeleteGenreHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(id, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("DeleteGenre")
            .WithSummary("Deletes a genre (albums referencing it are restricted by FK rule)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
