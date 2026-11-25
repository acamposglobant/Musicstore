namespace MusicStore.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using MusicStore.Application.Albums;
using MusicStore.Application.Songs;
using Xunit;

/// <summary>
/// End-to-end song CRUD through the YARP gateway (/songs/songs) against the
/// songs microservice (MIG-21), including the interval (TimeSpan) round-trip.
/// </summary>
[Collection("AppHost")]
public sealed class SongCrudTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetSongs_returns_seeded_songs()
    {
        var songs = await fixture.GatewayClient.GetFromJsonAsync<List<SongDto>>("/songs/songs");

        Assert.NotNull(songs);
        Assert.Contains(songs, s => s.Name == "City Lights");
        Assert.Contains(songs, s => s.Name == "Ode to Joy");
    }

    [Fact]
    public async Task GetSongsByAlbum_filters_by_album()
    {
        var songs = await fixture.GatewayClient.GetFromJsonAsync<List<SongDto>>("/songs/songs?albumId=1");

        Assert.NotNull(songs);
        Assert.All(songs, s => Assert.Equal(1, s.AlbumId));
        Assert.Contains(songs, s => s.Name == "City Lights");
    }

    [Fact]
    public async Task Song_crud_roundtrip_preserves_interval_duration()
    {
        // Arrange: parent album (with its own artist + genre for FK integrity).
        var album = await CreateAlbumAsync("Song Test Album");

        // Create — duration round-trips through PostgreSQL `interval`.
        var duration = new TimeSpan(0, 4, 12, 30);
        var createResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/songs/songs", new CreateSongRequest(album.Id, "Integration Song", duration));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SongDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(duration, created.Duration);

        // Read
        var fetched = await fixture.GatewayClient.GetFromJsonAsync<SongDto>(
            $"/songs/songs/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(duration, fetched.Duration);

        // Update
        var newDuration = new TimeSpan(0, 0, 5, 45);
        var updateResponse = await fixture.GatewayClient.PutAsJsonAsync(
            $"/songs/songs/{created.Id}", new UpdateSongRequest("Renamed Song", newDuration));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var renamed = await fixture.GatewayClient.GetFromJsonAsync<SongDto>(
            $"/songs/songs/{created.Id}");
        Assert.NotNull(renamed);
        Assert.Equal("Renamed Song", renamed.Name);
        Assert.Equal(newDuration, renamed.Duration);

        // Delete
        var deleteResponse = await fixture.GatewayClient.DeleteAsync(
            $"/songs/songs/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await fixture.GatewayClient.GetAsync(
            $"/songs/songs/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);

        // Cleanup parent chain.
        await fixture.GatewayClient.DeleteAsync($"/catalog/albums/{album.Id}");
        await fixture.GatewayClient.DeleteAsync($"/catalog/artists/{album.ArtistId}");
        await fixture.GatewayClient.DeleteAsync($"/catalog/genres/{album.GenreId}");
    }

    private async Task<AlbumDto> CreateAlbumAsync(string title)
    {
        var artistResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/artists", new MusicStore.Application.Artists.CreateArtistRequest("Song Test Artist"));
        artistResponse.EnsureSuccessStatusCode();
        var artist = (await artistResponse.Content.ReadFromJsonAsync<MusicStore.Application.Artists.ArtistDto>())!;

        var genreResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/genres", new MusicStore.Application.Genres.CreateGenreRequest("Song Test Genre", "Created by integration tests"));
        genreResponse.EnsureSuccessStatusCode();
        var genre = (await genreResponse.Content.ReadFromJsonAsync<MusicStore.Application.Genres.GenreDto>())!;

        var albumResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/albums", new CreateAlbumRequest(artist.Id, genre.Id, title));
        albumResponse.EnsureSuccessStatusCode();
        return (await albumResponse.Content.ReadFromJsonAsync<AlbumDto>())!;
    }
}
