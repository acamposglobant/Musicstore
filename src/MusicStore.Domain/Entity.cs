namespace MusicStore.Domain;

/// <summary>
/// Base type for catalog entities persisted with a database-generated integer key.
/// </summary>
public abstract class Entity
{
    /// <summary>Database-generated identifier; <c>0</c> means not yet persisted.</summary>
    public int Id { get; protected set; }
}
