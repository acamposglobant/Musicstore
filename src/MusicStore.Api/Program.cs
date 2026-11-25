using MusicStore.Api.Endpoints;
using MusicStore.Api.ErrorHandling;
using MusicStore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// PostgreSQL data access registered by Infrastructure: singleton NpgsqlDataSource
// bound to the Aspire-injected connection string + Dapper snake_case conventions.
// No hardcoded paths — config flows from the AppHost.
builder.AddMusicStoreInfrastructure();

// Global RFC 9457 ProblemDetails error handling (MIG-17):
// NotFoundException -> 404, ValidationException -> 422, unexpected -> 500 (no details leaked).
builder.AddMusicStoreErrorHandling();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMusicStoreErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new { service = "MusicStore.Api", status = "running" }))
    .WithName("GetRoot")
    .WithSummary("Service info root endpoint");

// Catalog CRUD (MIG-18): replaces the legacy GetArtist()/GetGenres() SOAP operations.
app.MapArtistEndpoints();
app.MapGenreEndpoints();

// Albums & Songs CRUD (MIG-19): enriched read path + nested songs collection.
app.MapAlbumEndpoints();
app.MapSongEndpoints();

app.Run();
