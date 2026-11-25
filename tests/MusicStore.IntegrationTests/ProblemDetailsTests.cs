namespace MusicStore.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

/// <summary>
/// Asserts RFC 9457 ProblemDetails error responses through the gateway —
/// the typed-contract replacement for the legacy ServiceReturn/ExceptionReturn
/// envelope (KeyNotFoundException → 404, ValidationException → 422).
/// </summary>
[Collection("AppHost")]
public sealed class ProblemDetailsTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Get_missing_artist_returns_404_problem()
    {
        var response = await fixture.GatewayClient.GetAsync("/catalog/artists/999999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "https://tools.ietf.org/html/rfc9110#section-15.5.5");
    }

    [Fact]
    public async Task Get_missing_genre_returns_404_problem()
    {
        var response = await fixture.GatewayClient.GetAsync("/catalog/genres/999999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_missing_album_returns_404_problem()
    {
        var response = await fixture.GatewayClient.GetAsync("/catalog/albums/999999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_missing_song_returns_404_problem()
    {
        var response = await fixture.GatewayClient.GetAsync("/songs/songs/999999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_artist_with_empty_name_returns_422_problem()
    {
        var response = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/artists", new { name = string.Empty });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_song_with_zero_duration_returns_422_problem()
    {
        // Arrange: a valid parent album is required to reach song validation.
        var album = await CreateAlbumAsync("Problem Details Test Album");

        var response = await fixture.GatewayClient.PostAsJsonAsync(
            "/songs/songs", new { albumId = album.Id, name = "Zero Duration", duration = TimeSpan.Zero });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);

        // Cleanup parent chain.
        await fixture.GatewayClient.DeleteAsync($"/catalog/albums/{album.Id}");
        await fixture.GatewayClient.DeleteAsync($"/catalog/artists/{album.ArtistId}");
        await fixture.GatewayClient.DeleteAsync($"/catalog/genres/{album.GenreId}");
    }

    [Fact]
    public async Task Problem_response_has_rfc9457_content_type()
    {
        var response = await fixture.GatewayClient.GetAsync("/catalog/artists/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? expectedType = null)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var problem = await JsonDocument.ParseAsync(stream);

        Assert.True(problem.RootElement.TryGetProperty("title", out var title));
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()));

        if (expectedType is not null)
        {
            Assert.True(problem.RootElement.TryGetProperty("type", out var type));
            Assert.Equal(expectedType, type.GetString());
        }
    }

    private async Task<MusicStore.Application.Albums.AlbumDto> CreateAlbumAsync(string title)
    {
        var artistResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/artists", new { name = "Problem Details Artist" });
        artistResponse.EnsureSuccessStatusCode();
        var artist = (await artistResponse.Content.ReadFromJsonAsync<MusicStore.Application.Artists.ArtistDto>())!;

        var genreResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/genres", new { name = "Problem Details Genre", description = "Created by integration tests" });
        genreResponse.EnsureSuccessStatusCode();
        var genre = (await genreResponse.Content.ReadFromJsonAsync<MusicStore.Application.Genres.GenreDto>())!;

        var albumResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/albums", new { artistId = artist.Id, genreId = genre.Id, title });
        albumResponse.EnsureSuccessStatusCode();
        return (await albumResponse.Content.ReadFromJsonAsync<MusicStore.Application.Albums.AlbumDto>())!;
    }
}
