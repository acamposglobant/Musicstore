namespace MusicStore.Application.UnitTests.Albums;

using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Application.Albums;
using MusicStore.Domain;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

public sealed class GetAlbumsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAllAlbumsMappedToDtos()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var albums = new List<Album>
        {
            new(1, 10, 20, "Album One"),
            new(2, 11, 21, "Album Two"),
        };
        albumRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(albums);
        var handler = new GetAlbumsHandler(albumRepository);

        var result = await handler.HandleAsync();

        var expected = new List<AlbumDto>
        {
            new(1, 10, 20, "Album One"),
            new(2, 11, 21, "Album Two"),
        };
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_EmptyRepository_ReturnsEmptyList()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        albumRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetAlbumsHandler(albumRepository);

        var result = await handler.HandleAsync();

        Assert.Empty(result);
    }
}

public sealed class GetAlbumsWithDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAlbumsWithArtistAndGenreNames()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artist = new Artist(10, "Artist One");
        var genre = new Genre(20, "Rock", "Rock music");
        var albums = new List<Album> { new(1, artist, genre, "Album One") };
        albumRepository.GetAllWithArtistAndGenreAsync(Arg.Any<CancellationToken>()).Returns(albums);
        var handler = new GetAlbumsWithDetailsHandler(albumRepository);

        var result = await handler.HandleAsync();

        var expected = new List<AlbumDetailsDto>
        {
            new(1, 10, "Artist One", 20, "Rock", "Album One"),
        };
        Assert.Equal(expected, result);
    }
}

public sealed class GetAlbumByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingAlbum_ReturnsDto()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        albumRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Album(1, 10, 20, "Album One"));
        var handler = new GetAlbumByIdHandler(albumRepository);

        var result = await handler.HandleAsync(1);

        Assert.Equal(new AlbumDto(1, 10, 20, "Album One"), result);
    }

    [Fact]
    public async Task HandleAsync_UnknownId_ThrowsNotFound()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        albumRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new GetAlbumByIdHandler(albumRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));
    }
}

public sealed class CreateAlbumHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsCreatedAlbumWithId()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artistRepository = Substitute.For<IArtistRepository>();
        var genreRepository = Substitute.For<IGenreRepository>();
        artistRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(new Artist(10, "Artist One"));
        genreRepository.GetByIdAsync(20, Arg.Any<CancellationToken>()).Returns(new Genre(20, "Rock", "Rock music"));
        albumRepository.AddAsync(Arg.Any<Album>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var album = callInfo.Arg<Album>();
                return new Album(42, album.ArtistId, album.GenreId, album.Title);
            });
        var handler = new CreateAlbumHandler(albumRepository, artistRepository, genreRepository);

        var result = await handler.HandleAsync(new CreateAlbumRequest(10, 20, "New Album"));

        Assert.Equal(new AlbumDto(42, 10, 20, "New Album"), result);
        await albumRepository.Received(1).AddAsync(
            Arg.Is<Album>(a => a.ArtistId == 10 && a.GenreId == 20 && a.Title == "New Album"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownArtist_ThrowsValidation()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artistRepository = Substitute.For<IArtistRepository>();
        var genreRepository = Substitute.For<IGenreRepository>();
        artistRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new CreateAlbumHandler(albumRepository, artistRepository, genreRepository);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(new CreateAlbumRequest(99, 20, "X")));

        Assert.Contains("Artist 99", exception.Message);
        await albumRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_UnknownGenre_ThrowsValidation()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artistRepository = Substitute.For<IArtistRepository>();
        var genreRepository = Substitute.For<IGenreRepository>();
        artistRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(new Artist(10, "Artist One"));
        genreRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new CreateAlbumHandler(albumRepository, artistRepository, genreRepository);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(new CreateAlbumRequest(10, 99, "X")));

        await albumRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}

public sealed class UpdateAlbumHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingAlbum_UpdatesTitleAndSaves()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artistRepository = Substitute.For<IArtistRepository>();
        var genreRepository = Substitute.For<IGenreRepository>();
        var album = new Album(1, 10, 20, "Old Title");
        albumRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(album);
        artistRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(new Artist(10, "Artist One"));
        genreRepository.GetByIdAsync(20, Arg.Any<CancellationToken>()).Returns(new Genre(20, "Rock", "Rock music"));
        var handler = new UpdateAlbumHandler(albumRepository, artistRepository, genreRepository);

        await handler.HandleAsync(1, new UpdateAlbumRequest(10, 20, "New Title"));

        Assert.Equal("New Title", album.Title);
        await albumRepository.Received(1).UpdateAsync(album, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownAlbum_ThrowsNotFound()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artistRepository = Substitute.For<IArtistRepository>();
        var genreRepository = Substitute.For<IGenreRepository>();
        albumRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new UpdateAlbumHandler(albumRepository, artistRepository, genreRepository);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(99, new UpdateAlbumRequest(10, 20, "X")));

        await albumRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_UnknownArtistOnUpdate_ThrowsValidation()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artistRepository = Substitute.For<IArtistRepository>();
        var genreRepository = Substitute.For<IGenreRepository>();
        albumRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Album(1, 10, 20, "Album One"));
        artistRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new UpdateAlbumHandler(albumRepository, artistRepository, genreRepository);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(1, new UpdateAlbumRequest(99, 20, "X")));
    }

    [Fact]
    public async Task HandleAsync_UnknownGenreOnUpdate_ThrowsValidation()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var artistRepository = Substitute.For<IArtistRepository>();
        var genreRepository = Substitute.For<IGenreRepository>();
        albumRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Album(1, 10, 20, "Album One"));
        artistRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(new Artist(10, "Artist One"));
        genreRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new UpdateAlbumHandler(albumRepository, artistRepository, genreRepository);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(1, new UpdateAlbumRequest(10, 99, "X")));

        await albumRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }
}

public sealed class DeleteAlbumHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingAlbum_Deletes()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        var album = new Album(1, 10, 20, "Album One");
        albumRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(album);
        var handler = new DeleteAlbumHandler(albumRepository);

        await handler.HandleAsync(1);

        await albumRepository.Received(1).DeleteAsync(album, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownAlbum_ThrowsNotFound()
    {
        var albumRepository = Substitute.For<IAlbumRepository>();
        albumRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new DeleteAlbumHandler(albumRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));

        await albumRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
