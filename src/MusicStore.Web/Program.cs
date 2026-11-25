using MusicStore.Web;
using MusicStore.Web.Components;
using MusicStore.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OTel tracing, health endpoints (/health, /alive),
// resilience on outbound HttpClient calls (MIG-20 parity for the UI host).
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Typed client for the YARP gateway. The base address is injected by the AppHost
// via WithReference(gateway) as services__gateway__http__0 (service discovery);
// no hardcoded URLs. Resilience (retry/CB/timeout) comes from ServiceDefaults.
builder.Services.AddHttpClient<CatalogApiClient>(client =>
{
    // "http://gateway" resolves via Aspire service discovery; falls back to
    // localhost:5000-style config for standalone runs.
    client.BaseAddress = new Uri(builder.Configuration["Catalog:GatewayBaseUrl"] ?? "http://gateway");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Required by the Blazor Web App template in this SDK version (form posts /
// EditForm emit anti-forgery metadata on every endpoint).
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Aspire health endpoints (/health, /alive) for the dashboard.
app.MapDefaultEndpoints();

app.Run();
