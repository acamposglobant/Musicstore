namespace MusicStore.IntegrationTests;

using System.Net;
using Xunit;

/// <summary>
/// MIG-34: UI integration coverage — gateway → UI smoke tests.
///
/// The Blazor web app is a real resource in the AppHost (waits for the gateway,
/// resolves it via service discovery). These tests verify the UI host serves its
/// pages and that the OpenAPI contract of the APIs behind the gateway stays the
/// source of truth the UI consumes.
///
/// Note: with Interactive Server rendering, data arrives over SignalR after the
/// initial HTML — so these tests assert page availability, status codes and the
/// app shell, not rendered rows (the data path gateway→API is covered by the
/// CRUD suites; the UI's typed client targets the same gateway routes).
[Collection("AppHost")]
public sealed class UiSmokeTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Home_page_renders_app_shell()
    {
        var response = await fixture.WebClient.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MusicStore", html);
    }

    [Fact]
    public async Task All_browse_pages_are_routable()
    {
        foreach (var url in new[] { "/", "/artists", "/genres", "/albums" })
        {
            using var response = await fixture.WebClient.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode, $"{url} returned {(int)response.StatusCode}");
        }
    }

    [Fact]
    public async Task Album_detail_route_is_routable()
    {
        using var response = await fixture.WebClient.GetAsync("/albums/1");
        Assert.True(response.IsSuccessStatusCode, $"/albums/1 returned {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Unknown_route_falls_back_to_not_found_page()
    {
        using var response = await fixture.WebClient.GetAsync("/this-page-does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Web_app_health_endpoints_are_exposed()
    {
        // ServiceDefaults: /health and /alive are the Aspire health probes the
        // dashboard (and WaitFor) rely on.
        using var alive = await fixture.WebClient.GetAsync("/alive");
        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);

        using var health = await fixture.WebClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Fact]
    public async Task OpenAPI_contract_remains_source_of_truth()
    {
        // The UI consumes the gateway routes; the APIs' OpenAPI documents remain
        // the contract. Assert both APIs still expose their OpenAPI documents
        // through the gateway (contract stays the source of truth, MIG-34 DoD).
        using var catalogOpenApi = await fixture.GatewayClient.GetAsync("/catalog/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, catalogOpenApi.StatusCode);

        using var songsOpenApi = await fixture.GatewayClient.GetAsync("/songs/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, songsOpenApi.StatusCode);
    }

    [Fact]
    public async Task Gateway_routes_reachable_from_ui_host()
    {
        // The UI's CatalogApiClient targets the gateway; verify the exact routes
        // the UI calls are served by the gateway (contract the UI depends on).
        foreach (var url in new[]
                 {
                     "/catalog/artists",
                     "/catalog/genres",
                     "/catalog/albums/with-details",
                     "/songs/songs?albumId=1",
                 })
        {
            using var response = await fixture.GatewayClient.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode, $"GET {url} -> {(int)response.StatusCode}");
        }
    }
}
