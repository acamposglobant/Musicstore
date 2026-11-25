namespace MusicStore.Gateway;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the YARP reverse proxy with Aspire service discovery.
/// Route map (replaces the legacy single SOAP endpoint):
///   /catalog/{**catch-all} -> catalog API (MusicStore.Api)
///   /songs/{**catch-all}   -> songs API  (MusicStore.Songs.Api)
/// </summary>
public static class DependencyInjection
{
    public static IHostApplicationBuilder AddMusicStoreGateway(this IHostApplicationBuilder builder)
    {
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        // Service discovery resolves the "https://catalog" / "https://songs"
        // destinations against Aspire-injected service endpoints.
        builder.Services.AddServiceDiscovery();

        return builder;
    }
}
