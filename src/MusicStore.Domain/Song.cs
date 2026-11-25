namespace MusicStore.Domain;

/// <summary>
/// A song on an album. Legacy: <c>Song(SongID, AlbumID, Name nvarchar(50), Duration time(7))</c>;
/// duration is a true <see cref="TimeSpan"/> (PostgreSQL <c>interval</c>), not a time-of-day.
/// </summary>
public class Song : Entity
{
    public const int NameMaxLength = 50;

    /// <summary>Parameterless constructor for materializers (Dapper).</summary>
    private Song()
    {
    }

    public Song(Album album, string name, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(album);
        SetName(name);
        SetDuration(duration);

        this.Album = album;
        this.AlbumId = album.Id;

        album.songs.Add(this);
    }

    /// <summary>Creates a new song from its album id only (API create path; album not loaded).</summary>
    public Song(int albumId, string name, TimeSpan duration)
    {
        if (albumId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(albumId), "Album id must be positive.");
        }

        SetName(name);
        SetDuration(duration);
        this.AlbumId = albumId;
    }

    /// <summary>Reconstitutes an existing song from persistence.</summary>
    public Song(int id, int albumId, string name, TimeSpan duration)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persisted song id must be positive.");
        }

        if (albumId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(albumId), "Album id must be positive.");
        }

        SetName(name);
        SetDuration(duration);
        this.Id = id;
        this.AlbumId = albumId;
    }

    public int AlbumId { get; private set; }

    public string Name { get; private set; } = null!;

    public TimeSpan Duration { get; private set; }

    public Album Album { get; private set; } = null!;

    public void UpdateDetails(string name, TimeSpan duration)
    {
        SetName(name);
        SetDuration(duration);
    }

    private void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException($"Song name exceeds {NameMaxLength} characters.", nameof(name));
        }

        this.Name = name.Trim();
    }

    private void SetDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Song duration must be positive.");
        }

        this.Duration = duration;
    }
}
