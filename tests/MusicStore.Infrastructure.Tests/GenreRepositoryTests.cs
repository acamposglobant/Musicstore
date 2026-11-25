namespace MusicStore.Infrastructure.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MusicStore.Domain;
using MusicStore.Infrastructure.Repositories;
using Xunit;

/// <summary>
/// CRUD + snake_case mapping tests for <see cref="GenreRepository"/> against a
/// real PostgreSQL database (legacy <c>UnitTestGenre.cs</c> replacement).
/// </summary>
public sealed class GenreRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private GenreRepository CreateRepository() => new(fixture.DataSource, NullLogger<GenreRepository>.Instance);

    [Fact]
    public async Task AddAsync_persists_name_and_description()
    {
        var repository = this.CreateRepository();

        var added = await repository.AddAsync(new Genre("Test Genre Alpha", "A genre created by tests"), TestContext.Current.CancellationToken);

        Assert.True(added.Id > 0);
        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal("Test Genre Alpha", loaded.Name);
        Assert.Equal("A genre created by tests", loaded.Description);
    }

    [Fact]
    public async Task GetAllAsync_returns_seeded_genres()
    {
        var repository = this.CreateRepository();

        var genres = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Contains(genres, genre => genre.Name == "Rock");
        Assert.Contains(genres, genre => genre.Name == "Jazz");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        var repository = this.CreateRepository();

        Assert.Null(await repository.GetByIdAsync(int.MaxValue, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_changes_description()
    {
        var repository = this.CreateRepository();
        var added = await repository.AddAsync(new Genre("Test Genre Beta", "before"), TestContext.Current.CancellationToken);

        await repository.UpdateAsync(new Genre(added.Id, "Test Genre Beta", "after"), TestContext.Current.CancellationToken);

        var loaded = await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal("after", loaded.Description);
    }

    [Fact]
    public async Task UpdateAsync_throws_for_missing_genre()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<MusicStore.Application.Common.NotFoundException>(
            () => repository.UpdateAsync(new Genre(int.MaxValue, "Ghost", "Ghost"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_removes_genre()
    {
        var repository = this.CreateRepository();
        var added = await repository.AddAsync(new Genre("Test Genre Doomed", "doomed"), TestContext.Current.CancellationToken);

        await repository.DeleteAsync(added, TestContext.Current.CancellationToken);

        Assert.Null(await repository.GetByIdAsync(added.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_throws_for_missing_genre()
    {
        var repository = this.CreateRepository();

        await Assert.ThrowsAsync<MusicStore.Application.Common.NotFoundException>(
            () => repository.DeleteAsync(new Genre(int.MaxValue, "Ghost", "Ghost"), TestContext.Current.CancellationToken));
    }
}
