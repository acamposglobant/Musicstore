namespace MusicStore.Gateway;

using Microsoft.Extensions.ServiceDiscovery.Http;
using Yarp.ReverseProxy.Forwarder;

/// <summary>
/// Replaces YARP's default forwarder HTTP client factory so the forwarding pipeline
/// resolves service-discovery hosts ("http://catalog" -> actual endpoint).
/// YARP does not go through IHttpClientFactory, so the service-discovery delegating
/// handler registered by ConfigureHttpClientDefaults (ServiceDefaults) never applies
/// to proxied requests; this factory attaches it explicitly.
/// </summary>
public sealed class ServiceDiscoveryForwarderHttpClientFactory(
    IServiceDiscoveryHttpMessageHandlerFactory serviceDiscoveryHandlerFactory) : IForwarderHttpClientFactory
{
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        var socketsHandler = new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.None,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        };

        // Fresh resolving handler per cluster client: each gets its own resolution pipeline.
        var resolvingHandler = serviceDiscoveryHandlerFactory.CreateHandler(socketsHandler);

        return new HttpMessageInvoker(resolvingHandler, disposeHandler: true);
    }
}
