namespace MusicStore.Application.UnitTests.Genres;

using MusicStore.Application.Common;
using MusicStore.Application.Genres;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

public sealed class GetGenresHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAllGenresMappedToDtos()
    {
        var repository = Substitute.For<IGenreRepository>();
        var genres = new List<Genre>
        {
            new(1, "Genre One", "Description One"),
            new(2, "Genre Two", "Description Two"),
        };
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(genres);
        var handler = new GetGenresHandler(repository);

        var result = await handler.HandleAsync();

        var expected = new List<GenreDto>
        {
            new(1, "Genre One", "Description One"),
            new(2, "Genre Two", "Description Two"),
        };
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_EmptyRepository_ReturnsEmptyList()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetGenresHandler(repository);

        var result = await handler.HandleAsync();

        Assert.Empty(result);
    }
}

public sealed class GetGenreByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingGenre_ReturnsDto()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Genre(1, "Genre One", "Description One"));
        var handler = new GetGenreByIdHandler(repository);

        var result = await handler.HandleAsync(1);

        Assert.Equal(new GenreDto(1, "Genre One", "Description One"), result);
    }

    [Fact]
    public async Task HandleAsync_MissingGenre_ThrowsNotFound()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new GetGenreByIdHandler(repository);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));

        Assert.Contains("Genre", exception.Message);
        Assert.Contains("99", exception.Message);
    }
}

public sealed class CreateGenreHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesAndReturnsDtoWithGeneratedId()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.AddAsync(Arg.Any<Genre>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var genre = callInfo.Arg<Genre>();
                return new Genre(99, genre.Name, genre.Description);
            });
        var handler = new CreateGenreHandler(repository);

        var result = await handler.HandleAsync(new CreateGenreRequest("Genre One", "Description One"));

        Assert.Equal(new GenreDto(99, "Genre One", "Description One"), result);
    }

    [Fact]
    public async Task HandleAsync_PassesNameAndDescriptionToRepository()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.AddAsync(Arg.Any<Genre>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Genre>());
        var handler = new CreateGenreHandler(repository);

        _ = await handler.HandleAsync(new CreateGenreRequest("Genre One", "Description One"));

        await repository.Received(1).AddAsync(
            Arg.Is<Genre>(g => g.Name == "Genre One" && g.Description == "Description One"),
            Arg.Any<CancellationToken>());
    }
}

public sealed class UpdateGenreHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingGenre_UpdatesDetails()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Genre(1, "Genre One", "Description One"));
        var handler = new UpdateGenreHandler(repository);

        await handler.HandleAsync(1, new UpdateGenreRequest("Renamed", "New Description"));

        await repository.Received(1).UpdateAsync(
            Arg.Is<Genre>(g => g.Id == 1 && g.Name == "Renamed" && g.Description == "New Description"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingGenre_ThrowsNotFound()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new UpdateGenreHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(99, new UpdateGenreRequest("Renamed", "New Description")));

        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }
}

public sealed class DeleteGenreHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingGenre_DeletesIt()
    {
        var repository = Substitute.For<IGenreRepository>();
        var genre = new Genre(1, "Genre One", "Description One");
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(genre);
        var handler = new DeleteGenreHandler(repository);

        await handler.HandleAsync(1);

        await repository.Received(1).DeleteAsync(genre, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingGenre_ThrowsNotFound()
    {
        var repository = Substitute.For<IGenreRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new DeleteGenreHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));

        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
