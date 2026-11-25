namespace MusicStore.Api.ErrorHandling;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Registers global RFC 9457 ProblemDetails error handling (MIG-17).
/// </summary>
public static class ErrorHandlingExtensions
{
    public static IHostApplicationBuilder AddMusicStoreErrorHandling(this IHostApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options =>
        {
            // Fallback for exceptions that did not go through the handler
            // (e.g. middleware-level failures): same mapping, no details leaked.
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            };
        });

        builder.Services.AddExceptionHandler<MusicStoreExceptionHandler>();

        return builder;
    }

    public static WebApplication UseMusicStoreErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();

        // RFC 9457 bodies for framework-generated errors (404 route misses, 405, ...).
        app.UseStatusCodePages();

        return app;
    }
}
