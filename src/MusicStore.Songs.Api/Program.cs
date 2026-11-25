using Microsoft.AspNetCore.HttpLogging;
using MusicStore.Api.Endpoints;
using MusicStore.Api.ErrorHandling;
using MusicStore.Application;
using MusicStore.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// Serilog (MIG-35): structured logging with console + file sinks, enriched with
// machine name and correlation id. Config lives in appsettings.json (Serilog
// section) — no hardcoded paths. Coexists with the OTel exporter from
// ServiceDefaults (traces/metrics) — Serilog owns the text logs.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName());

// PostgreSQL data access registered by Infrastructure: singleton NpgsqlDataSource
// bound to the Aspire-injected connection string + Dapper snake_case conventions.
// The songs service connects with the musicstore_songs role (see db/300_songs_role.sql).
builder.AddMusicStoreInfrastructure();

// Application-layer vertical-slice handlers (MIG-16). Without this registration,
// minimal APIs infer handler parameters as body parameters and GET routes fail
// at initialization ("Body was inferred but the method does not allow...").
builder.Services.AddMusicStoreApplication();

// Global RFC 9457 ProblemDetails error handling (MIG-17):
// NotFoundException -> 404, ValidationException -> 422, unexpected -> 500 (no details leaked).
builder.AddMusicStoreErrorHandling();

builder.Services.AddOpenApi();

// Request logging (structured) — replaces the legacy WrapperLogger/NLog tracing.
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders
                            | HttpLoggingFields.ResponsePropertiesAndHeaders;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMusicStoreErrorHandling();

// Serilog request logging (MIG-35): one structured line per request. The
// correlation id middleware below enriches it (and every downstream log line)
// with a per-request CorrelationId via LogContext.
app.UseSerilogRequestLogging();
app.Use(async (context, next) =>
{
    var incoming = context.Request.Headers["CorrelationId"].ToString();
    var correlationId = incoming.Length > 0 ? incoming : context.TraceIdentifier;
    context.Response.Headers["CorrelationId"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next(context);
    }
});
app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // OpenAPI UI (dev only): Scalar explorer over the /openapi/v1.json document.
    app.MapScalarApiReference();
}

// Health checks for the Aspire dashboard (/health readiness + /alive liveness).
app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new { service = "MusicStore.Songs.Api", status = "running" }))
    .WithName("GetRoot")
    .WithSummary("Service info root endpoint");

// Songs CRUD (MIG-21): the songs microservice owns all song operations.
// GET /songs?albumId={id} replaces the catalog's GET /albums/{id}/songs route.
app.MapSongEndpoints();

app.Run();
