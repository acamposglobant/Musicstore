namespace MusicStore.Application.UnitTests.Songs;

using MusicStore.Application.Common;
using MusicStore.Application.Interfaces;
using MusicStore.Application.Songs;
using MusicStore.Domain;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

public sealed class GetSongsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAllSongsMappedToDtos()
    {
        var repository = Substitute.For<ISongRepository>();
        var songs = new List<Song>
        {
            new(1, 10, "Song One", TimeSpan.FromMinutes(3)),
            new(2, 10, "Song Two", TimeSpan.FromMinutes(4)),
        };
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(songs);
        var handler = new GetSongsHandler(repository);

        var result = await handler.HandleAsync();

        var expected = new List<SongDto>
        {
            new(1, 10, "Song One", TimeSpan.FromMinutes(3)),
            new(2, 10, "Song Two", TimeSpan.FromMinutes(4)),
        };
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_EmptyRepository_ReturnsEmptyList()
    {
        var repository = Substitute.For<ISongRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetSongsHandler(repository);

        var result = await handler.HandleAsync();

        Assert.Empty(result);
    }
}

public sealed class GetSongsByAlbumHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOnlySongsOfTheAlbum()
    {
        var repository = Substitute.For<ISongRepository>();
        var songs = new List<Song> { new(1, 10, "Song One", TimeSpan.FromMinutes(3)) };
        repository.GetByAlbumIdAsync(10, Arg.Any<CancellationToken>()).Returns(songs);
        var handler = new GetSongsByAlbumHandler(repository);

        var result = await handler.HandleAsync(10);

        var expected = new List<SongDto> { new(1, 10, "Song One", TimeSpan.FromMinutes(3)) };
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleAsync_PassesAlbumIdToRepository()
    {
        var repository = Substitute.For<ISongRepository>();
        repository.GetByAlbumIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetSongsByAlbumHandler(repository);

        _ = await handler.HandleAsync(42);

        await repository.Received(1).GetByAlbumIdAsync(42, Arg.Any<CancellationToken>());
    }
}

public sealed class GetSongByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingSong_ReturnsDto()
    {
        var repository = Substitute.For<ISongRepository>();
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Song(1, 10, "Song One", TimeSpan.FromMinutes(3)));
        var handler = new GetSongByIdHandler(repository);

        var result = await handler.HandleAsync(1);

        Assert.Equal(new SongDto(1, 10, "Song One", TimeSpan.FromMinutes(3)), result);
    }

    [Fact]
    public async Task HandleAsync_MissingSong_ThrowsNotFound()
    {
        var repository = Substitute.For<ISongRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new GetSongByIdHandler(repository);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));

        Assert.Contains("Song", exception.Message);
        Assert.Contains("99", exception.Message);
    }
}

public sealed class CreateSongHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingAlbum_CreatesAndReturnsDto()
    {
        var songRepository = Substitute.For<ISongRepository>();
        var albumRepository = Substitute.For<IAlbumRepository>();
        albumRepository.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(new Album(10, 1, 1, "Album"));
        songRepository.AddAsync(Arg.Any<Song>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var song = callInfo.Arg<Song>();
                return new Song(99, song.AlbumId, song.Name, song.Duration);
            });
        var handler = new CreateSongHandler(songRepository, albumRepository);

        var result = await handler.HandleAsync(
            new CreateSongRequest(10, "Song One", TimeSpan.FromMinutes(3)));

        Assert.Equal(new SongDto(99, 10, "Song One", TimeSpan.FromMinutes(3)), result);
    }

    [Fact]
    public async Task HandleAsync_ExistingAlbum_AddsSongWithRequestValues()
    {
        var songRepository = Substitute.For<ISongRepository>();
        var albumRepository = Substitute.For<IAlbumRepository>();
        albumRepository.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(new Album(10, 1, 1, "Album"));
        songRepository.AddAsync(Arg.Any<Song>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Song>());
        var handler = new CreateSongHandler(songRepository, albumRepository);

        _ = await handler.HandleAsync(
            new CreateSongRequest(10, "Song One", TimeSpan.FromMinutes(3)));

        await songRepository.Received(1).AddAsync(
            Arg.Is<Song>(s => s.AlbumId == 10 && s.Name == "Song One" && s.Duration == TimeSpan.FromMinutes(3)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingAlbum_ThrowsValidation()
    {
        var songRepository = Substitute.For<ISongRepository>();
        var albumRepository = Substitute.For<IAlbumRepository>();
        albumRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new CreateSongHandler(songRepository, albumRepository);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(new CreateSongRequest(99, "Song One", TimeSpan.FromMinutes(3))));

        Assert.Contains("Album 99 does not exist", exception.Message);
        await songRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}

public sealed class UpdateSongHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingSong_UpdatesDetails()
    {
        var repository = Substitute.For<ISongRepository>();
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Song(1, 10, "Song One", TimeSpan.FromMinutes(3)));
        var handler = new UpdateSongHandler(repository);

        await handler.HandleAsync(1, new UpdateSongRequest("Renamed", TimeSpan.FromMinutes(5)));

        await repository.Received(1).UpdateAsync(
            Arg.Is<Song>(s => s.Id == 1 && s.Name == "Renamed" && s.Duration == TimeSpan.FromMinutes(5)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingSong_ThrowsNotFound()
    {
        var repository = Substitute.For<ISongRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new UpdateSongHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.HandleAsync(99, new UpdateSongRequest("Renamed", TimeSpan.FromMinutes(5))));

        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }
}

public sealed class DeleteSongHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingSong_DeletesIt()
    {
        var repository = Substitute.For<ISongRepository>();
        var song = new Song(1, 10, "Song One", TimeSpan.FromMinutes(3));
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(song);
        var handler = new DeleteSongHandler(repository);

        await handler.HandleAsync(1);

        await repository.Received(1).DeleteAsync(song, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingSong_ThrowsNotFound()
    {
        var repository = Substitute.For<ISongRepository>();
        repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();
        var handler = new DeleteSongHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(99));

        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
