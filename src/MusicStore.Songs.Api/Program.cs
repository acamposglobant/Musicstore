using Microsoft.AspNetCore.HttpLogging;
using MusicStore.Api.Endpoints;
using MusicStore.Api.ErrorHandling;
using MusicStore.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// PostgreSQL data access registered by Infrastructure: singleton NpgsqlDataSource
// bound to the Aspire-injected connection string + Dapper snake_case conventions.
// The songs service connects with the musicstore_songs role (see db/300_songs_role.sql).
builder.AddMusicStoreInfrastructure();

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
