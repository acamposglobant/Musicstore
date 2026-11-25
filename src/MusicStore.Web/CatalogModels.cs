namespace MusicStore.Web.Services;

using System.Net;

/// <summary>Artist read model as served by the catalog API (camelCase JSON).</summary>
public sealed record ArtistDto(int Id, string Name);

/// <summary>Read model for a genre.</summary>
public sealed record GenreDto(int Id, string Name, string Description);

/// <summary>Read model for an album, enriched with artist/genre names.</summary>
public sealed record AlbumDto(int Id, int ArtistId, int GenreId, string Title, string ArtistName, string GenreName);

/// <summary>Read model for a song.</summary>
public sealed record SongDto(int Id, int AlbumId, string Name, TimeSpan Duration);

/// <summary>
/// RFC 9457 ProblemDetails payload — surfaced as user-visible UI error messages
/// (MIG-31 DoD: ProblemDetails surfaced as UI error messages).
/// </summary>
public sealed record ProblemDetails(string? Title, string? Detail, int Status);

/// <summary>
/// Thrown by <see cref="CatalogApiClient"/> when the gateway returns an error
/// response. Carries the parsed ProblemDetails (title/detail) for UI display.
/// </summary>
public sealed class CatalogApiException(HttpStatusCode statusCode, ProblemDetails? problem) : Exception
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public ProblemDetails? Problem { get; } = problem;

    /// <summary>Maps the exception to a user-visible message (detail → title → status fallback).</summary>
    public string Describe() => this.Problem?.Detail
        ?? this.Problem?.Title
        ?? $"The request failed (HTTP {(int)this.StatusCode}).";
}
