using MusicStore.Gateway;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// YARP reverse proxy: single front door replacing the legacy IIS/WCF host
// (MusicStoreService.svc). Destinations resolve via Aspire service discovery
// (https://catalog / https://songs), not hardcoded hosts/ports.
builder.AddMusicStoreGateway();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.Run();
