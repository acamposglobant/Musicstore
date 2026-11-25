namespace MusicStore.Infrastructure.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MusicStore.Domain;
using MusicStore.Infrastructure.Repositories;
using Xunit;

/// <summary>
/// Tests for <see cref="AlbumRepository"/> against a real PostgreSQL database,
/// including the Dapper multi-mapping read path that replaces the legacy EDMX
/// navigation-property loads (legacy <c>UnitTestAlbum.cs</c> replacement).
/// </summary>
public sealed class AlbumRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private AlbumRepository CreateRepository() => new(fixture.DataSource, NullLogger<AlbumRepository>.Instance);

    [Fact]
    public async Task AddAsync_persists_album_with_foreign_keys()
    {
        var repository = this.CreateRepository();
        var (artistId, genreId) = await this.CreateArtistAndGenreAsync();

        var added = await repository.AddAsync(new Album(artistId, genreId, "Test Album Alpha"), TestContext.Current.CancellationToken);

        Assert.True(added.Id > 0);
        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(artistId, loaded.ArtistId);
        Assert.Equal(genreId, loaded.GenreId);
        Assert.Equal("Test Album Alpha", loaded.Title);
    }

    [Fact]
    public async Task GetAllAsync_returns_seeded_albums()
    {
        var repository = this.CreateRepository();

        var albums = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Contains(albums, album => album.Title == "Neon Horizons");
        Assert.Contains(albums, album => album.Title == "Symphony No. 9");
    }

    [Fact]
    public async Task GetAllWithArtistAndGenreAsync_splits_joined_rows_into_album_artist_genre()
    {
        var repository = this.CreateRepository();

        var albums = await repository.GetAllWithArtistAndGenreAsync(TestContext.Current.CancellationToken);

        // Multi-mapping: each album carries its joined Artist and Genre entities.
        var neon = Assert.Single(albums, album => album.Title == "Neon Horizons");
        Assert.NotNull(neon.Artist);
        Assert.Equal("The Midnight Echo", neon.Artist.Name);
        Assert.NotNull(neon.Genre);
        Assert.Equal("Rock", neon.Genre.Name);
        Assert.Equal("Classic and modern rock music", neon.Genre.Description);

        // Identity split must not bleed artist columns into genre (or vice versa).
        var symphony = Assert.Single(albums, album => album.Title == "Symphony No. 9");
        Assert.Equal("Ludwig van Beethoven", symphony.Artist.Name);
        Assert.Equal("Classical", symphony.Genre.Name);
    }

    [Fact]
    public async Task GetAllWithArtistAndGenreAsync_maps_newly_added_album()
    {
        var repository = this.CreateRepository();
        var (artistId, genreId) = await this.CreateArtistAndGenreAsync();
        var added = await repository.AddAsync(new Album(artistId, genreId, "Multi-Mapped Album"), TestContext.Current.CancellationToken);

        var albums = await repository.GetAllWithArtistAndGenreAsync(TestContext.Current.CancellationToken);

        var mapped = Assert.Single(albums, album => album.Title == "Multi-Mapped Album");
        Assert.Equal(added.Id, mapped.Id);
        Assert.Equal("Multi-Mapped Artist", mapped.Artist.Name);
        Assert.Equal("Multi-Mapped Genre", mapped.Genre.Name);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        var repository = this.CreateRepository();

        Assert.Null(await repository.GetByIdAsync(int.MaxValue, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_changes_title()
    {
        var repository = this.CreateRepository();
        var (artistId, genreId) = await this.CreateArtistAndGenreAsync();
        var added = await repository.AddAsync(new Album(artistId, genreId, "Before Title"), TestContext.Current.CancellationToken);

        await repository.UpdateAsync(new Album(added.Id, artistId, genreId, "After Title"), TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal("After Title", loaded.Title);
    }

    [Fact]
    public async Task UpdateAsync_throws_for_missing_album()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<MusicStore.Application.Common.NotFoundException>(
            () => repository.UpdateAsync(new Album(int.MaxValue, 1, 1, "Ghost"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_removes_album()
    {
        var repository = this.CreateRepository();
        var (artistId, genreId) = await this.CreateArtistAndGenreAsync();
        var added = await repository.AddAsync(new Album(artistId, genreId, "Doomed Album"), TestContext.Current.CancellationToken);

        await repository.DeleteAsync(added, TestContext.Current.CancellationToken);

        Assert.Null(await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_throws_for_missing_album()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<MusicStore.Application.Common.NotFoundException>(
            () => repository.DeleteAsync(new Album(int.MaxValue, 1, 1, "Ghost"), TestContext.Current.CancellationToken));
    }

    private async Task<(int ArtistId, int GenreId)> CreateArtistAndGenreAsync()
    {
        var artist = await new ArtistRepository(fixture.DataSource, NullLogger<ArtistRepository>.Instance).AddAsync(new Artist("Multi-Mapped Artist"), TestContext.Current.CancellationToken);
        var genre = await new GenreRepository(fixture.DataSource, NullLogger<GenreRepository>.Instance).AddAsync(new Genre("Multi-Mapped Genre", "created by tests"), TestContext.Current.CancellationToken);
        return (artist.Id, genre.Id);
    }
}
