namespace MusicStore.Infrastructure.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MusicStore.Domain;
using MusicStore.Infrastructure.Repositories;
using Xunit;

/// <summary>
/// Tests for <see cref="SongRepository"/> against a real PostgreSQL database,
/// focused on the <see cref="TimeSpan"/> ↔ <c>interval</c> round-trip that
/// replaces the legacy <c>time(7)</c> column (legacy <c>UnitTestSong.cs</c>
/// replacement).
/// </summary>
public sealed class SongRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private SongRepository CreateRepository() => new(fixture.DataSource, NullLogger<SongRepository>.Instance);

    [Fact]
    public async Task AddAsync_round_trips_duration_as_interval()
    {
        var repository = this.CreateRepository();
        var albumId = await this.CreateAlbumAsync();
        var duration = new TimeSpan(0, 3, 42, 0);

        var added = await repository.AddAsync(new Song(albumId, "Round Trip Song", duration), TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(duration, loaded.Duration);
    }

    [Fact]
    public async Task Duration_survives_sub_second_precision()
    {
        var repository = this.CreateRepository();
        var albumId = await this.CreateAlbumAsync();
        var duration = new TimeSpan(0, 0, 4, 5, 123);

        var added = await repository.AddAsync(new Song(albumId, "Precision Song", duration), TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(duration, loaded.Duration);
    }

    [Fact]
    public async Task Durations_beyond_24_hours_are_representable()
    {
        // Legacy time(7) wrapped at 24h; interval does not.
        var repository = this.CreateRepository();
        var albumId = await this.CreateAlbumAsync();
        var duration = new TimeSpan(1, 6, 30, 0);

        var added = await repository.AddAsync(new Song(albumId, "Long Form Song", duration), TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(duration, loaded.Duration);
    }

    [Fact]
    public async Task GetByAlbumIdAsync_returns_only_that_albums_songs()
    {
        var repository = this.CreateRepository();
        var albumId = await this.CreateAlbumAsync();
        var otherAlbumId = await this.CreateAlbumAsync();
        await repository.AddAsync(new Song(albumId, "Mine", TimeSpan.FromMinutes(2)), TestContext.Current.CancellationToken);
        await repository.AddAsync(new Song(otherAlbumId, "Theirs", TimeSpan.FromMinutes(3)), TestContext.Current.CancellationToken);

        var songs = await repository.GetByAlbumIdAsync(albumId, TestContext.Current.CancellationToken);

        var song = Assert.Single(songs);
        Assert.Equal("Mine", song.Name);
        Assert.Equal(albumId, song.AlbumId);
    }

    [Fact]
    public async Task GetAllAsync_returns_seeded_songs_with_durations()
    {
        var repository = this.CreateRepository();

        var songs = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        var cityLights = Assert.Single(songs, song => song.Name == "City Lights");
        Assert.Equal(new TimeSpan(0, 0, 3, 42, 0), cityLights.Duration);
    }

    [Fact]
    public async Task UpdateAsync_changes_name_and_duration()
    {
        var repository = this.CreateRepository();
        var albumId = await this.CreateAlbumAsync();
        var added = await repository.AddAsync(new Song(albumId, "Before", TimeSpan.FromMinutes(1)), TestContext.Current.CancellationToken);
        var newDuration = new TimeSpan(0, 0, 7, 1, 250);

        await repository.UpdateAsync(new Song(added.Id, albumId, "After", newDuration), TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal("After", loaded.Name);
        Assert.Equal(newDuration, loaded.Duration);
    }

    [Fact]
    public async Task UpdateAsync_throws_for_missing_song()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<MusicStore.Application.Common.NotFoundException>(
            () => repository.UpdateAsync(new Song(int.MaxValue, 1, "Ghost", TimeSpan.FromMinutes(1)), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_removes_song()
    {
        var repository = this.CreateRepository();
        var albumId = await this.CreateAlbumAsync();
        var added = await repository.AddAsync(new Song(albumId, "Doomed Song", TimeSpan.FromMinutes(1)), TestContext.Current.CancellationToken);

        await repository.DeleteAsync(added, TestContext.Current.CancellationToken);

        Assert.Null(await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_throws_for_missing_song()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<MusicStore.Application.Common.NotFoundException>(
            () => repository.DeleteAsync(new Song(int.MaxValue, 1, "Ghost", TimeSpan.FromMinutes(1)), TestContext.Current.CancellationToken));
    }

    private async Task<int> CreateAlbumAsync()
    {
        var artist = await new ArtistRepository(fixture.DataSource, NullLogger<ArtistRepository>.Instance).AddAsync(new Artist("Song Test Artist"), TestContext.Current.CancellationToken);
        var genre = await new GenreRepository(fixture.DataSource, NullLogger<GenreRepository>.Instance).AddAsync(new Genre("Song Test Genre", "created by tests"), TestContext.Current.CancellationToken);
        var album = await new AlbumRepository(fixture.DataSource, NullLogger<AlbumRepository>.Instance).AddAsync(new Album(artist.Id, genre.Id, "Song Test Album"), TestContext.Current.CancellationToken);
        return album.Id;
    }
}
