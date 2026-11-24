namespace MusicStore.Domain;

/// <summary>
/// A music artist. Legacy: <c>Artist(ArtistID, Name nchar(100))</c>.
/// </summary>
public class Artist : Entity
{
    public const int NameMaxLength = 100;

    internal readonly List<Album> albums = [];

    /// <summary>Parameterless constructor for materializers (Dapper); bypasses nothing — use constructors to create valid instances.</summary>
    private Artist()
    {
    }

    public Artist(string name)
    {
        SetName(name);
    }

    public string Name { get; private set; } = null!;

    public IReadOnlyCollection<Album> Albums => this.albums;

    public void Rename(string name) => SetName(name);

    private void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException($"Artist name exceeds {NameMaxLength} characters.", nameof(name));
        }

        this.Name = name.Trim();
    }
}
