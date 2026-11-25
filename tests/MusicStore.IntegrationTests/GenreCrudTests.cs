namespace MusicStore.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using MusicStore.Application.Genres;
using Xunit;

/// <summary>
/// End-to-end genre CRUD through the YARP gateway (/catalog/genres).
/// </summary>
[Collection("AppHost")]
public sealed class GenreCrudTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetGenres_returns_seeded_genres()
    {
        var genres = await fixture.GatewayClient.GetFromJsonAsync<List<GenreDto>>("/catalog/genres");

        Assert.NotNull(genres);
        Assert.Contains(genres, g => g.Name == "Rock");
        Assert.Contains(genres, g => g.Name == "Jazz");
    }

    [Fact]
    public async Task Genre_crud_roundtrip_through_gateway()
    {
        // Create
        var createResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/genres", new CreateGenreRequest("Integration Genre", "Created by integration tests"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GenreDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Integration Genre", created.Name);

        // Read
        var fetched = await fixture.GatewayClient.GetFromJsonAsync<GenreDto>(
            $"/catalog/genres/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);

        // Update
        var updateResponse = await fixture.GatewayClient.PutAsJsonAsync(
            $"/catalog/genres/{created.Id}", new UpdateGenreRequest("Renamed Genre", "Updated description"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var renamed = await fixture.GatewayClient.GetFromJsonAsync<GenreDto>(
            $"/catalog/genres/{created.Id}");
        Assert.NotNull(renamed);
        Assert.Equal("Renamed Genre", renamed.Name);
        Assert.Equal("Updated description", renamed.Description);

        // Delete
        var deleteResponse = await fixture.GatewayClient.DeleteAsync(
            $"/catalog/genres/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await fixture.GatewayClient.GetAsync(
            $"/catalog/genres/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }
}
