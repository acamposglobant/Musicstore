namespace MusicStore.Gateway;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Identifies which downstream API a <see cref="DownstreamHealthCheck"/> probes.</summary>
public sealed record DownstreamHealthCheckDescriptor(string ServiceName, string HttpClientName);

/// <summary>Catalog API downstream probe configuration.</summary>
public sealed class DownstreamHealthCheckCatalogDescriptor
{
    public string ServiceName => "catalog";
    public string HttpClientName => "catalog-health";
}

/// <summary>Songs API downstream probe configuration.</summary>
public sealed class DownstreamHealthCheckSongsDescriptor
{
    public string ServiceName => "songs";
    public string HttpClientName => "songs-health";
}

/// <summary>
/// Probes one downstream API's /health endpoint through the gateway's
/// service-discovery-resolved, resilience-wrapped HttpClient.
/// Replaces the legacy WCF world where downstream health was invisible
/// (myWCFMessages.xml tracing had no liveness semantics).
/// </summary>
public sealed class DownstreamHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly string serviceName;
    private readonly string httpClientName;
    private readonly ILogger logger;

    public DownstreamHealthCheck(
        IHttpClientFactory httpClientFactory,
        DownstreamHealthCheckCatalogDescriptor descriptor,
        ILogger<DownstreamHealthCheck> logger)
        : this(httpClientFactory, descriptor.ServiceName, descriptor.HttpClientName, logger)
    {
    }

    public DownstreamHealthCheck(
        IHttpClientFactory httpClientFactory,
        DownstreamHealthCheckSongsDescriptor descriptor,
        ILogger<DownstreamHealthCheck> logger)
        : this(httpClientFactory, descriptor.ServiceName, descriptor.HttpClientName, logger)
    {
    }

    private DownstreamHealthCheck(
        IHttpClientFactory httpClientFactory,
        string serviceName,
        string httpClientName,
        ILogger logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.serviceName = serviceName;
        this.httpClientName = httpClientName;
        this.logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = this.httpClientFactory.CreateClient(this.httpClientName);
            using var response = await httpClient.GetAsync("/health", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"{this.serviceName} is healthy");
            }

            return HealthCheckResult.Degraded($"{this.serviceName} /health returned {(int)response.StatusCode}");
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "Downstream health probe failed for {Service}", this.serviceName);
            return HealthCheckResult.Unhealthy($"{this.serviceName} unreachable: {exception.Message}");
        }
    }
}
