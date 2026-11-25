namespace MusicStore.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using MusicStore.Application.Albums;
using MusicStore.Application.Artists;
using MusicStore.Application.Genres;
using Xunit;

/// <summary>
/// End-to-end album CRUD through the YARP gateway (/catalog/albums),
/// including the enriched read path (multi-mapping artist/genre names).
/// </summary>
[Collection("AppHost")]
public sealed class AlbumCrudTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetAlbums_returns_seeded_albums()
    {
        var albums = await fixture.GatewayClient.GetFromJsonAsync<List<AlbumDto>>("/catalog/albums");

        Assert.NotNull(albums);
        Assert.Contains(albums, a => a.Title == "Neon Horizons");
        Assert.Contains(albums, a => a.Title == "Symphony No. 9");
    }

    [Fact]
    public async Task GetAlbumsWithDetails_includes_artist_and_genre_names()
    {
        var albums = await fixture.GatewayClient.GetFromJsonAsync<List<AlbumDetailsDto>>(
            "/catalog/albums/with-details");

        Assert.NotNull(albums);
        var neon = Assert.Single(albums, a => a.Title == "Neon Horizons");
        Assert.Equal("The Midnight Echo", neon.ArtistName);
        Assert.Equal("Rock", neon.GenreName);
    }

    [Fact]
    public async Task Album_crud_roundtrip_through_gateway()
    {
        // Arrange: parent artist + genre for FK integrity.
        var artist = await CreateArtistAsync("Album Test Artist");
        var genre = await CreateGenreAsync("Album Test Genre");

        // Create
        var createResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/albums", new CreateAlbumRequest(artist.Id, genre.Id, "Integration Album"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AlbumDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Integration Album", created.Title);

        // Read
        var fetched = await fixture.GatewayClient.GetFromJsonAsync<AlbumDto>(
            $"/catalog/albums/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(artist.Id, fetched.ArtistId);
        Assert.Equal(genre.Id, fetched.GenreId);

        // Update
        var updateResponse = await fixture.GatewayClient.PutAsJsonAsync(
            $"/catalog/albums/{created.Id}", new UpdateAlbumRequest(artist.Id, genre.Id, "Renamed Album"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var renamed = await fixture.GatewayClient.GetFromJsonAsync<AlbumDto>(
            $"/catalog/albums/{created.Id}");
        Assert.NotNull(renamed);
        Assert.Equal("Renamed Album", renamed.Title);

        // Delete
        var deleteResponse = await fixture.GatewayClient.DeleteAsync(
            $"/catalog/albums/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await fixture.GatewayClient.GetAsync(
            $"/catalog/albums/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);

        // Cleanup parents.
        await fixture.GatewayClient.DeleteAsync($"/catalog/artists/{artist.Id}");
        await fixture.GatewayClient.DeleteAsync($"/catalog/genres/{genre.Id}");
    }

    private async Task<ArtistDto> CreateArtistAsync(string name)
    {
        var response = await fixture.GatewayClient.PostAsJsonAsync("/catalog/artists", new CreateArtistRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ArtistDto>())!;
    }

    private async Task<GenreDto> CreateGenreAsync(string name)
    {
        var response = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/genres", new CreateGenreRequest(name, "Created by integration tests"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GenreDto>())!;
    }
}
