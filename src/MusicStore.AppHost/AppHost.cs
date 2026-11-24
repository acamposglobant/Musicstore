var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("musicstore-db")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("musicstore-db-database", "musicstore");

builder.Build().Run();
