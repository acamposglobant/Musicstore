namespace MusicStore.Application.Common;

/// <summary>
/// Thrown when a requested entity does not exist. Mapped to HTTP 404 by the API layer (MIG-17).
/// Replaces the legacy <c>ServiceReturn("ERROR")</c> + <c>KeyNotFoundException</c> pattern.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}
