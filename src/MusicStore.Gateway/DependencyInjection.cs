namespace MusicStore.Gateway;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery;
using Yarp.ReverseProxy.Forwarder;

/// <summary>
/// Registers the YARP reverse proxy with Aspire service discovery.
/// Route map (replaces the legacy single SOAP endpoint):
///   /catalog/{**catch-all} -> catalog API (MusicStore.Api)
///   /songs/{**catch-all}   -> songs API  (MusicStore.Songs.Api)
/// Destinations use http: the gateway is the TLS edge; internal
/// service-to-service traffic stays on the container network.
/// </summary>
public static class DependencyInjection
{
    public static IHostApplicationBuilder AddMusicStoreGateway(this IHostApplicationBuilder builder)
    {
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        // Service discovery for the gateway's own HttpClient instances (health probes).
        // YARP's forwarder does NOT use the IHttpClientFactory pipeline; the custom
        // IForwarderHttpClientFactory below attaches service discovery to it so
        // destinations like "http://catalog" resolve against Aspire-injected endpoints.
        builder.Services.AddServiceDiscovery();
        builder.Services.AddSingleton<IForwarderHttpClientFactory, ServiceDiscoveryForwarderHttpClientFactory>();

        return builder;
    }
}
