namespace MusicStore.Application.UnitTests.Artists;

using MusicStore.Application.Artists;
using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Domain;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

public sealed class GetArtistsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAllArtistsMappedToDtos()
    {
        var repository = Substitute.For<IArtistRepository>();
        var artists = new List<Artist> { new(1, "Artist One"), new(2, "Artist Two") };
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(artists);
        var handler = new GetArtistsHandler(repository);

        var result = await handler.HandleAsync();

        var expected = new List<ArtistDto> { new(1, "Artist One"), new(2, "Artist Two") };
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_EmptyRepository_ReturnsEmptyList()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetArtistsHandler(repository);

        var result = await handler.HandleAsync();

        Assert.Empty(result);
    }
}

public sealed class GetArtistByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingArtist_ReturnsDto()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Artist(1, "Artist One"));
        var handler = new GetArtistByIdHandler(repository);

        var result = await handler.HandleAsync(1);

        Assert.Equal(new ArtistDto(1, "Artist One"), result);
    }

    [Fact]
    public async Task HandleAsync_MissingArtist_ThrowsNotFound()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new GetArtistByIdHandler(repository);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));

        Assert.Contains("Artist", exception.Message);
        Assert.Contains("99", exception.Message);
    }
}

public sealed class CreateArtistHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesAndReturnsDtoWithGeneratedId()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.AddAsync(Arg.Any<Artist>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var artist = callInfo.Arg<Artist>();
                return new Artist(99, artist.Name);
            });
        var handler = new CreateArtistHandler(repository);

        var result = await handler.HandleAsync(new CreateArtistRequest("Artist One"));

        Assert.Equal(new ArtistDto(99, "Artist One"), result);
    }

    [Fact]
    public async Task HandleAsync_PassesArtistNameToRepository()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.AddAsync(Arg.Any<Artist>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Artist>());
        var handler = new CreateArtistHandler(repository);

        _ = await handler.HandleAsync(new CreateArtistRequest("Artist One"));

        await repository.Received(1).AddAsync(
            Arg.Is<Artist>(a => a.Name == "Artist One"),
            Arg.Any<CancellationToken>());
    }
}

public sealed class UpdateArtistHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingArtist_RenamesIt()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Artist(1, "Artist One"));
        var handler = new UpdateArtistHandler(repository);

        await handler.HandleAsync(1, new UpdateArtistRequest("Renamed"));

        await repository.Received(1).UpdateAsync(
            Arg.Is<Artist>(a => a.Id == 1 && a.Name == "Renamed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingArtist_ThrowsNotFound()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new UpdateArtistHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(99, new UpdateArtistRequest("Renamed")));

        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }
}

public sealed class DeleteArtistHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingArtist_DeletesIt()
    {
        var repository = Substitute.For<IArtistRepository>();
        var artist = new Artist(1, "Artist One");
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(artist);
        var handler = new DeleteArtistHandler(repository);

        await handler.HandleAsync(1);

        await repository.Received(1).DeleteAsync(artist, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingArtist_ThrowsNotFound()
    {
        var repository = Substitute.For<IArtistRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new DeleteArtistHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));

        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
