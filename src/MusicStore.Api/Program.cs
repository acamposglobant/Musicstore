using MusicStore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// PostgreSQL data access registered by Infrastructure: singleton NpgsqlDataSource
// bound to the Aspire-injected connection string + Dapper snake_case conventions.
// No hardcoded paths — config flows from the AppHost.
builder.AddMusicStoreInfrastructure();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new { service = "MusicStore.Api", status = "running" }))
    .WithName("GetRoot")
    .WithSummary("Service info root endpoint");

app.Run();
