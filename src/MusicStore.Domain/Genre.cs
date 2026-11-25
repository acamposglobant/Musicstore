namespace MusicStore.Domain;

/// <summary>
/// A music genre (shared reference data). Legacy: <c>Genre(GenreID, Name nchar(50), Description nchar(100))</c>.
/// </summary>
public class Genre : Entity
{
    public const int NameMaxLength = 50;
    public const int DescriptionMaxLength = 100;

    internal readonly List<Album> albums = [];

    /// <summary>Parameterless constructor for materializers (Dapper).</summary>
    private Genre()
    {
    }

    public Genre(string name, string description)
    {
        SetName(name);
        SetDescription(description);
    }

    /// <summary>Reconstitutes an existing genre from persistence.</summary>
    public Genre(int id, string name, string description)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persisted genre id must be positive.");
        }

        SetName(name);
        SetDescription(description);
        this.Id = id;
    }

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public IReadOnlyCollection<Album> Albums => this.albums;

    public void UpdateDetails(string name, string description)
    {
        SetName(name);
        SetDescription(description);
    }

    private void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException($"Genre name exceeds {NameMaxLength} characters.", nameof(name));
        }

        this.Name = name.Trim();
    }

    private void SetDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (description.Length > DescriptionMaxLength)
        {
            throw new ArgumentException($"Genre description exceeds {DescriptionMaxLength} characters.", nameof(description));
        }

        this.Description = description.Trim();
    }
}
