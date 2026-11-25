namespace MusicStore.IntegrationTests;

using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Shared fixture: boots the Aspire AppHost once per test collection (real PostgreSQL
/// container, DbUp migrations, catalog API, songs API, YARP gateway) and exposes an
/// <see cref="HttpClient"/> bound to the gateway resource — the single front door that
/// replaces the legacy WCF host.
/// </summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    private DistributedApplication? app;

    public IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;

    public DistributedApplication App => app ?? throw new InvalidOperationException("AppHost not started yet.");

    public HttpClient GatewayClient { get; private set; } = null!;

    /// <summary>HttpClient bound to the Blazor web app resource (MIG-34 UI smoke tests).</summary>
    public HttpClient WebClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MusicStore_AppHost>();
        Builder.Services.AddHttpClient();

        app = await Builder.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications.WaitForResourceHealthyAsync("gateway");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("web");

        GatewayClient = app.CreateHttpClient("gateway");
        WebClient = app.CreateHttpClient("web");
    }

    public async Task DisposeAsync()
    {
        GatewayClient.Dispose();
        WebClient.Dispose();
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }

        await Builder.DisposeAsync();
    }

    /// <summary>
    /// Parses an RFC 9457 ProblemDetails body for error-path assertions.
    /// </summary>
    public static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
