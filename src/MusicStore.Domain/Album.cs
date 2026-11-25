namespace MusicStore.Domain;

/// <summary>
/// An album by an artist, belonging to a genre. Legacy: <c>Album(AlbumID, GenreID, ArtistID, Title nchar(100))</c>.
/// </summary>
public class Album : Entity
{
    public const int TitleMaxLength = 100;

    internal readonly List<Song> songs = [];

    /// <summary>Parameterless constructor for materializers (Dapper).</summary>
    private Album()
    {
    }

    public Album(Artist artist, Genre genre, string title)
    {
        ArgumentNullException.ThrowIfNull(artist);
        ArgumentNullException.ThrowIfNull(genre);
        SetTitle(title);

        this.Artist = artist;
        this.ArtistId = artist.Id;
        this.Genre = genre;
        this.GenreId = genre.Id;

        artist.albums.Add(this);
        genre.albums.Add(this);
    }

    /// <summary>Creates a new album from foreign-key ids only (API create path; entities not loaded).</summary>
    public Album(int artistId, int genreId, string title)
    {
        if (artistId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(artistId), "Artist id must be positive.");
        }

        if (genreId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(genreId), "Genre id must be positive.");
        }

        SetTitle(title);
        this.ArtistId = artistId;
        this.GenreId = genreId;
    }

    /// <summary>Reconstitutes an existing album from persistence with its owning references.</summary>
    public Album(int id, int artistId, int genreId, string title)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persisted album id must be positive.");
        }

        if (artistId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(artistId), "Artist id must be positive.");
        }

        if (genreId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(genreId), "Genre id must be positive.");
        }

        SetTitle(title);
        this.Id = id;
        this.ArtistId = artistId;
        this.GenreId = genreId;
    }

    /// <summary>
    /// Reconstitutes an existing album from persistence together with its
    /// artist and genre (multi-mapping read path); navigation collections are
    /// not wired — load songs explicitly via the song repository.
    /// </summary>
    public Album(int id, Artist artist, Genre genre, string title)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persisted album id must be positive.");
        }

        ArgumentNullException.ThrowIfNull(artist);
        ArgumentNullException.ThrowIfNull(genre);
        if (artist.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(artist), "Artist id must be positive.");
        }

        if (genre.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(genre), "Genre id must be positive.");
        }

        SetTitle(title);
        this.Id = id;
        this.Artist = artist;
        this.ArtistId = artist.Id;
        this.Genre = genre;
        this.GenreId = genre.Id;
    }

    public int ArtistId { get; private set; }

    public int GenreId { get; private set; }

    public string Title { get; private set; } = null!;

    public Artist Artist { get; private set; } = null!;

    public Genre Genre { get; private set; } = null!;

    public IReadOnlyCollection<Song> Songs => this.songs;

    public void UpdateTitle(string title) => SetTitle(title);

    /// <summary>Reassigns the album to a different artist and/or genre (review H2).</summary>
    public void Reassign(int artistId, int genreId)
    {
        if (artistId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(artistId), "Artist id must be positive.");
        }

        if (genreId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(genreId), "Genre id must be positive.");
        }

        this.ArtistId = artistId;
        this.GenreId = genreId;
    }

    private void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (title.Length > TitleMaxLength)
        {
            throw new ArgumentException($"Album title exceeds {TitleMaxLength} characters.", nameof(title));
        }

        this.Title = title.Trim();
    }
}
