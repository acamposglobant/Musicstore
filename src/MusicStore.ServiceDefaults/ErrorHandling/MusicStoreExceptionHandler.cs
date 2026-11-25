namespace MusicStore.Api.ErrorHandling;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MusicStore.Application.Common;

/// <summary>
/// Maps Application-layer exceptions to RFC 9457 ProblemDetails responses.
/// Replaces the legacy <c>BaseService.LogServiceException</c> + <c>ExceptionReturn</c>
/// pattern (and <c>includeExceptionDetailInFaults=true</c>, which leaked stack traces).
/// </summary>
public sealed class MusicStoreExceptionHandler : IExceptionHandler
{
    private readonly ILogger<MusicStoreExceptionHandler> logger;

    public MusicStoreExceptionHandler(ILogger<MusicStoreExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status422UnprocessableEntity, "Validation failed"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            // No stack traces / exception details in responses (legacy debt:
            // includeExceptionDetailInFaults=true). Details go to the log only.
            this.logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            this.logger.LogWarning(
                exception,
                "Request to {Path} failed with {Status}: {Message}",
                httpContext.Request.Path,
                status,
                exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = status switch
            {
                StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9457#section-15.5.5",
                StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc9457#section-15.5.21",
                _ => "https://tools.ietf.org/html/rfc9457#section-15.6.1",
            },
        };

        // Client errors carry the reason; server errors never leak exception details.
        if (status != StatusCodes.Status500InternalServerError)
        {
            problemDetails.Detail = exception.Message;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
