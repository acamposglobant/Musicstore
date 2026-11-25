namespace MusicStore.Gateway;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Health check registrations for the gateway: one probe per downstream API.
/// The Aspire dashboard's readiness view (/health) turns degraded when any
/// downstream API is unhealthy.
/// </summary>
public static class HealthCheckRegistration
{
    public static IHostApplicationBuilder AddGatewayHealthChecks(this IHostApplicationBuilder builder)
    {
        // Named clients inherit ConfigureHttpClientDefaults: service discovery
        // (http://catalog -> actual endpoint) + standard resilience handler
        // (retry / circuit-breaker / timeout) on every probe. The BaseAddress
        // host ("catalog"/"songs") is what service discovery resolves.
        builder.Services.AddHttpClient("catalog-health", client => client.BaseAddress = new Uri("http://catalog"));
        builder.Services.AddHttpClient("songs-health", client => client.BaseAddress = new Uri("http://songs"));

        // Type-activated registration: each probe gets its own instance configured
        // for its downstream API (the type has a single DI-resolvable constructor).
        builder.Services.AddHealthChecks()
            .AddTypeActivatedCheck<DownstreamHealthCheck>(
                "catalog-downstream",
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                ["ready"],
                DownstreamHealthCheckCatalogDescriptor.ServiceNameValue,
                DownstreamHealthCheckCatalogDescriptor.HttpClientNameValue)
            .AddTypeActivatedCheck<DownstreamHealthCheck>(
                "songs-downstream",
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                ["ready"],
                DownstreamHealthCheckSongsDescriptor.ServiceNameValue,
                DownstreamHealthCheckSongsDescriptor.HttpClientNameValue);

        return builder;
    }
}
