namespace MusicStore.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using MusicStore.Application.Artists;
using Xunit;

/// <summary>
/// End-to-end artist CRUD through the YARP gateway (/catalog/artists),
/// replacing the legacy GetArtist() SOAP operation.
/// </summary>
[Collection("AppHost")]
public sealed class ArtistCrudTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetArtists_returns_seeded_artists()
    {
        var artists = await fixture.GatewayClient.GetFromJsonAsync<List<ArtistDto>>("/catalog/artists");

        Assert.NotNull(artists);
        Assert.Contains(artists, a => a.Name == "The Midnight Echo");
        Assert.Contains(artists, a => a.Name == "Ella Fitzgerald");
    }

    [Fact]
    public async Task Artist_crud_roundtrip_through_gateway()
    {
        // Create
        var createResponse = await fixture.GatewayClient.PostAsJsonAsync(
            "/catalog/artists", new CreateArtistRequest("Integration Test Artist"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ArtistDto>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Integration Test Artist", created.Name);

        // Read
        var fetched = await fixture.GatewayClient.GetFromJsonAsync<ArtistDto>(
            $"/catalog/artists/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);

        // Update
        var updateResponse = await fixture.GatewayClient.PutAsJsonAsync(
            $"/catalog/artists/{created.Id}", new UpdateArtistRequest("Renamed Artist"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var renamed = await fixture.GatewayClient.GetFromJsonAsync<ArtistDto>(
            $"/catalog/artists/{created.Id}");
        Assert.NotNull(renamed);
        Assert.Equal("Renamed Artist", renamed.Name);

        // Delete
        var deleteResponse = await fixture.GatewayClient.DeleteAsync(
            $"/catalog/artists/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await fixture.GatewayClient.GetAsync(
            $"/catalog/artists/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }
}
