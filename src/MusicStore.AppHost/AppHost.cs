var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("musicstore-db")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("musicstore-db-database", "musicstore");

builder.AddDotnetProject("migrations", "../MusicStore.DbMigrations/MusicStore.DbMigrations.csproj")
    .WithReference(database)
    .WaitFor(database);

builder.AddDotnetProject("api", "../MusicStore.Api/MusicStore.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

// Songs microservice (MIG-21): owns all song operations; catalog API no longer serves songs.
builder.AddDotnetProject("songs-api", "../MusicStore.Songs.Api/MusicStore.Songs.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
