var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("musicstore-db")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("musicstore-db-database", "musicstore");

builder.AddDotnetProject("migrations", "../MusicStore.DbMigrations/MusicStore.DbMigrations.csproj")
    .WithReference(database)
    .WaitFor(database);

// Resource names double as service-discovery hosts. AddDotnetProject resources do not
// infer launchSettings endpoints, so an explicit http endpoint is declared here; Aspire
// then binds the project via ASPNETCORE_URLS and services__* / endpoint references resolve.
var catalogApi = builder.AddDotnetProject("catalog", "../MusicStore.Api/MusicStore.Api.csproj")
    .WithHttpEndpoint()
    .WithReference(database)
    .WaitFor(database);

// Songs microservice (MIG-21): owns all song operations; catalog API no longer serves songs.
var songsApi = builder.AddDotnetProject("songs", "../MusicStore.Songs.Api/MusicStore.Songs.Api.csproj")
    .WithHttpEndpoint()
    .WithReference(database)
    .WaitFor(database);

// YARP gateway (MIG-22): single front door — /catalog/** -> catalog API, /songs/** -> songs API.
// Destinations ("http://catalog" / "http://songs") resolve via service discovery: the
// gateway's custom IForwarderHttpClientFactory attaches the discovery handler to YARP's
// forwarder pipeline. The gateway waits for both APIs.
builder.AddDotnetProject("gateway", "../MusicStore.Gateway/MusicStore.Gateway.csproj")
    .WithReference(catalogApi)
    .WithReference(songsApi)
    .WaitFor(catalogApi)
    .WaitFor(songsApi);

builder.Build().Run();
