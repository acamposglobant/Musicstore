namespace MusicStore.IntegrationTests;

using System.Net;
using Xunit;

/// <summary>
/// Verifies the Aspire resource graph comes up healthy: PostgreSQL, DbUp migrations,
/// catalog API, songs API and the YARP gateway.
/// </summary>
[Collection("AppHost")]
public sealed class HealthCheckTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Gateway_resource_reaches_healthy_state()
    {
        // The fixture already waited for this; re-asserting is idempotent and
        // verifies the resource notification stream reports a healthy gateway.
        await fixture.App.ResourceNotifications.WaitForResourceHealthyAsync("gateway");
    }

    [Fact]
    public async Task Gateway_health_endpoint_returns_healthy()
    {
        var response = await fixture.GatewayClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Gateway_aliveness_endpoint_returns_healthy()
    {
        var response = await fixture.GatewayClient.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
