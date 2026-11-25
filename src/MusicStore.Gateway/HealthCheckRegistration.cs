namespace MusicStore.Gateway;

using Microsoft.Extensions.DependencyInjection;

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
        // (https://catalog -> actual endpoint) + standard resilience handler
        // (retry / circuit-breaker / timeout) on every probe.
        builder.Services.AddHttpClient("catalog-health");
        builder.Services.AddHttpClient("songs-health");

        builder.Services.AddHealthChecks()
            .AddCheck<DownstreamHealthCheck>("catalog-downstream", tags: ["ready"])
            .AddCheck<DownstreamHealthCheck>("songs-downstream", tags: ["ready"]);

        // Per-check service name + client resolved from the health check's own scope.
        builder.Services.AddSingleton<DownstreamHealthCheckCatalogDescriptor>();
        builder.Services.AddSingleton<DownstreamHealthCheckSongsDescriptor>();

        return builder;
    }
}
