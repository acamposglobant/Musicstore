namespace MusicStore.Infrastructure;

using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicStore.Application.Interfaces;
using MusicStore.Infrastructure.Repositories;

/// <summary>
/// Registers the PostgreSQL data-access stack: a singleton <see cref="NpgsqlDataSource"/>
/// resolved from the Aspire-injected connection string, plus Dapper mapping conventions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Connection-string name injected by Aspire (<c>WithReference(database)</c> in the AppHost).
    /// Must match the database resource name: <c>musicstore-db-database</c>.
    /// </summary>
    public const string ConnectionStringName = "musicstore-db-database";

    public static IHostApplicationBuilder AddMusicStoreInfrastructure(this IHostApplicationBuilder builder)
    {
        // Registers NpgsqlDataSource as a singleton, bound to the Aspire-provided
        // connection string (no hardcoded paths — config flows from the AppHost).
        builder.AddNpgsqlDataSource(ConnectionStringName);

        // Dapper convention: PostgreSQL snake_case columns map to C# PascalCase
        // properties (artist_id -> Id via alias, name -> Name, ...).
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Repositories: scoped, connection-per-operation via the pooled data source.
        builder.Services.AddScoped<IArtistRepository, ArtistRepository>();
        builder.Services.AddScoped<IGenreRepository, GenreRepository>();
        builder.Services.AddScoped<IAlbumRepository, AlbumRepository>();
        builder.Services.AddScoped<ISongRepository, SongRepository>();

        return builder;
    }
}
