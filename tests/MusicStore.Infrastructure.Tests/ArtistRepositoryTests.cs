namespace MusicStore.Infrastructure.Tests;

using MusicStore.Domain;
using MusicStore.Infrastructure.Repositories;
using Xunit;

/// <summary>
/// CRUD + snake_case mapping tests for <see cref="ArtistRepository"/> against a
/// real PostgreSQL database (legacy <c>UnitTestArtist.cs</c> replacement).
/// </summary>
public sealed class ArtistRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private ArtistRepository CreateRepository() => new(fixture.DataSource);

    [Fact]
    public async Task AddAsync_persists_and_returns_identity_id()
    {
        var repository = this.CreateRepository();

        var added = await repository.AddAsync(new Artist("Test Artist Alpha"), TestContext.Current.CancellationToken);

        Assert.True(added.Id > 0);
        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal("Test Artist Alpha", loaded.Name);
    }

    [Fact]
    public async Task GetAllAsync_returns_seeded_artists()
    {
        var repository = this.CreateRepository();

        var artists = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Contains(artists, artist => artist.Name == "The Midnight Echo");
        Assert.Contains(artists, artist => artist.Name == "Ella Fitzgerald");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        var repository = this.CreateRepository();

        Assert.Null(await repository.GetByIdAsync(int.MaxValue, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_renames_artist()
    {
        var repository = this.CreateRepository();
        var added = await repository.AddAsync(new Artist("Before Rename"), TestContext.Current.CancellationToken);

        var updated = new Artist(added.Id, "After Rename");
        await repository.UpdateAsync(updated, TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal("After Rename", loaded.Name);
    }

    [Fact]
    public async Task UpdateAsync_throws_for_missing_artist()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.UpdateAsync(new Artist(int.MaxValue, "Ghost"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_removes_artist()
    {
        var repository = this.CreateRepository();
        var added = await repository.AddAsync(new Artist("Doomed Artist"), TestContext.Current.CancellationToken);

        await repository.DeleteAsync(added, TestContext.Current.CancellationToken);

        Assert.Null(await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_throws_for_missing_artist()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.DeleteAsync(new Artist(int.MaxValue, "Ghost"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Names_round_trip_without_nchar_padding()
    {
        // Legacy nchar(100) padded every name to 100 chars; varchar must not.
        var repository = this.CreateRepository();
        const string unpaddedName = "exact length";

        var added = await repository.AddAsync(new Artist(unpaddedName), TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(unpaddedName.Length, loaded.Name.Length);
    }
}
