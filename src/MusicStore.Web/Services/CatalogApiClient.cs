namespace MusicStore.Web.Services;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Typed client for the catalog + songs APIs via the YARP gateway (MIG-22).
/// All UI HTTP traffic goes through the gateway — never directly to the APIs.
/// Error responses (RFC 9457 ProblemDetails) are parsed and surfaced as
/// <see cref="CatalogApiException"/> so components can render user-visible messages.
/// </summary>
public sealed class CatalogApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HttpClient HttpClient { get; } = httpClient;

    // ---- Artists -------------------------------------------------------------

    public async Task<IReadOnlyList<ArtistDto>> GetArtistsAsync(CancellationToken cancellationToken = default)
        => await GetListAsync<ArtistDto>("/catalog/artists", cancellationToken);

    public async Task<ArtistDto> GetArtistAsync(int id, CancellationToken cancellationToken = default)
        => await GetSingleAsync<ArtistDto>($"/catalog/artists/{id}", cancellationToken);

    public async Task<ArtistDto> CreateArtistAsync(string name, CancellationToken cancellationToken = default)
        => await SendForJsonAsync<ArtistDto>(HttpMethod.Post, "/catalog/artists", new { name }, cancellationToken);

    public async Task UpdateArtistAsync(int id, string name, CancellationToken cancellationToken = default)
        => await SendAsync(HttpMethod.Put, $"/catalog/artists/{id}", new { name }, cancellationToken);

    public Task DeleteArtistAsync(int id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/catalog/artists/{id}", cancellationToken);

    // ---- Genres --------------------------------------------------------------

    public async Task<IReadOnlyList<GenreDto>> GetGenresAsync(CancellationToken cancellationToken = default)
        => await GetListAsync<GenreDto>("/catalog/genres", cancellationToken);

    public async Task<GenreDto> CreateGenreAsync(string name, string description, CancellationToken cancellationToken = default)
        => await SendForJsonAsync<GenreDto>(HttpMethod.Post, "/catalog/genres", new { name, description }, cancellationToken);

    public async Task UpdateGenreAsync(int id, string name, string description, CancellationToken cancellationToken = default)
        => await SendAsync(HttpMethod.Put, $"/catalog/genres/{id}", new { name, description }, cancellationToken);

    public Task DeleteGenreAsync(int id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/catalog/genres/{id}", cancellationToken);

    // ---- Albums ---------------------------------------------------------------

    public async Task<IReadOnlyList<AlbumDto>> GetAlbumsAsync(CancellationToken cancellationToken = default)
        => await GetListAsync<AlbumDto>("/catalog/albums", cancellationToken);

    public async Task<AlbumDto> GetAlbumAsync(int id, CancellationToken cancellationToken = default)
        => await GetSingleAsync<AlbumDto>($"/catalog/albums/{id}", cancellationToken);

    public Task DeleteAlbumAsync(int id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/catalog/albums/{id}", cancellationToken);

    /// <summary>Enriched read path: albums with artist/genre names resolved server-side.</summary>
    public async Task<IReadOnlyList<AlbumDto>> GetAlbumsWithDetailsAsync(CancellationToken cancellationToken = default)
        => await GetListAsync<AlbumDto>("/catalog/albums/with-details", cancellationToken);

    // ---- Songs -----------------------------------------------------------------

    public async Task<IReadOnlyList<SongDto>> GetSongsByAlbumAsync(int albumId, CancellationToken cancellationToken = default)
        => await GetListAsync<SongDto>($"/songs/songs?albumId={albumId}", cancellationToken);

    public Task DeleteSongAsync(int id, CancellationToken cancellationToken = default)
        => DeleteAsync($"/songs/songs/{id}", cancellationToken);

    // ---- Plumbing ---------------------------------------------------------------

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await this.HttpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var items = await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions, cancellationToken);
        return items ?? [];
    }

    private async Task<T> GetSingleAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await this.HttpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return payload ?? throw new CatalogApiException(response.StatusCode, null);
    }

    private async Task<TResponse> SendForJsonAsync<TResponse>(HttpMethod method, string url, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        using var response = await this.HttpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        return payload ?? throw new CatalogApiException(response.StatusCode, null);
    }

    private async Task SendAsync(HttpMethod method, string url, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        using var response = await this.HttpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await this.HttpClient.DeleteAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>
    /// Ensures success; on error parses the RFC 9457 ProblemDetails body and throws
    /// <see cref="CatalogApiException"/> carrying it (surfaced as UI error messages).
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            // Non-JSON error body (e.g. raw gateway 503) — leave problem null.
        }

        throw new CatalogApiException(response.StatusCode, problem);
    }
}
