namespace MusicStore.IntegrationTests;

using System.Text.Json;
using Xunit;

/// <summary>
/// MIG-27: legacy parity verification — the sign-off gate for Phase 7 (legacy decommission).
///
/// The legacy WCF contract (MusicStore.Services.Contracts.IMusicStoreService) exposed:
///   GetArtist()  -> ServiceReturn { status = "OK", responseData = [ { artistID, name } ] }
///   GetGenres()  -> ServiceReturn { status = "OK", responseData = [ { genreID, name, description } ] }
///
/// The migrated stack must serve the SAME seed data through the gateway:
///   GET /catalog/artists  -> [ { id, name } ]
///   GET /catalog/genres   -> [ { id, name, description } ]
///
/// These tests assert the parity contract: the seeded entities (db/100_seed.sql) are
/// present with their legacy ids and names. The shared test database also contains
/// entities created by other tests in the collection, so exact counts cannot be
/// asserted — a superset check is the correct parity contract here.
/// </summary>
[Collection("AppHost")]
public sealed class LegacyParityTests(AppHostFixture fixture)
{
    private static readonly (int Id, string Name)[] ExpectedArtists =
    [
        (1, "The Midnight Echo"),
        (2, "Ella Fitzgerald"),
        (3, "Kraftwerk"),
        (4, "Ludwig van Beethoven"),
    ];

    private static readonly (int Id, string Name, string Description)[] ExpectedGenres =
    [
        (1, "Rock", "Classic and modern rock music"),
        (2, "Jazz", "Improvised music over swing and blues harmonies"),
        (3, "Electronic", "Synthesizer-driven dance and ambient music"),
        (4, "Classical", "Orchestral and chamber works"),
    ];

    [Fact]
    public async Task GetArtists_parity_with_legacy_GetArtist()
    {
        var response = await fixture.GatewayClient.GetAsync("/catalog/artists");
        response.EnsureSuccessStatusCode();

        var artists = await ParseListAsync(response);
        Assert.NotEmpty(artists);

        // Every seeded (id, name) pair from the legacy GetArtist() payload
        // (shared seed data, db/100_seed.sql) is present in the REST response.
        foreach (var (expectedId, expectedName) in ExpectedArtists)
        {
            var artist = Assert.Single(artists, a => a.GetProperty("id").GetInt32() == expectedId);
            Assert.Equal(expectedName, artist.GetProperty("name").GetString());
        }
    }

    [Fact]
    public async Task GetGenres_parity_with_legacy_GetGenres()
    {
        var response = await fixture.GatewayClient.GetAsync("/catalog/genres");
        response.EnsureSuccessStatusCode();

        var genres = await ParseListAsync(response);
        Assert.NotEmpty(genres);

        // Every seeded (id, name, description) triple from the legacy GetGenres()
        // payload is present in the REST response.
        foreach (var (expectedId, expectedName, expectedDescription) in ExpectedGenres)
        {
            var genre = Assert.Single(genres, g => g.GetProperty("id").GetInt32() == expectedId);
            Assert.Equal(expectedName, genre.GetProperty("name").GetString());
            Assert.Equal(expectedDescription, genre.GetProperty("description").GetString());
        }
    }

    [Fact]
    public async Task Legacy_payload_shape_is_preserved_as_REST_resources()
    {
        // The legacy envelope ServiceReturn { status, responseData, exception } is
        // replaced by HTTP semantics: 200 + JSON array on success, RFC 9457 on error.
        // This test pins the success shape: a bare JSON array (no wrapper object).
        var response = await fixture.GatewayClient.GetAsync("/catalog/artists");
        response.EnsureSuccessStatusCode();

        var body = await ParseListAsync(response);
        Assert.NotEmpty(body);

        // Every element exposes exactly the legacy entity fields (id + name),
        // serialized camelCase — no ServiceReturn wrapper, no exception envelope.
        foreach (var artist in body)
        {
            Assert.True(artist.TryGetProperty("id", out _));
            Assert.True(artist.TryGetProperty("name", out _));
            Assert.Equal(2, artist.EnumerateObject().Count());
        }
    }

    /// <summary>
    /// Parses a JSON array response into detached elements (Clone() keeps them
    /// readable after the owning JsonDocument is disposed).
    /// </summary>
    private static async Task<List<JsonElement>> ParseListAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return [.. doc.RootElement.EnumerateArray().Select(e => e.Clone())];
    }
}
