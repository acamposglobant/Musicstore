namespace MusicStore.Application.Common;

/// <summary>
/// Thrown when a request fails validation (missing/invalid fields, unknown references).
/// Mapped to HTTP 422 by the API layer (MIG-17). Replaces the legacy pattern of
/// returning <c>ExceptionReturn</c> payloads with embedded error strings.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
