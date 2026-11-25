var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("musicstore-db")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("musicstore-db-database", "musicstore");

builder.AddDotnetProject("migrations", "../MusicStore.DbMigrations/MusicStore.DbMigrations.csproj")
    .WithReference(database)
    .WaitFor(database);

var catalogApi = builder.AddDotnetProject("api", "../MusicStore.Api/MusicStore.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

// Songs microservice (MIG-21): owns all song operations; catalog API no longer serves songs.
var songsApi = builder.AddDotnetProject("songs-api", "../MusicStore.Songs.Api/MusicStore.Songs.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

// YARP gateway (MIG-22): single front door — /catalog/** -> catalog API, /songs/** -> songs API.
// Destinations resolve via Aspire service discovery; the gateway waits for both APIs.
builder.AddDotnetProject("gateway", "../MusicStore.Gateway/MusicStore.Gateway.csproj")
    .WithReference(catalogApi)
    .WithReference(songsApi)
    .WaitFor(catalogApi)
    .WaitFor(songsApi);

builder.Build().Run();
