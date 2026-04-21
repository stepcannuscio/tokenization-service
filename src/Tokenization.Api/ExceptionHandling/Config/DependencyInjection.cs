using System.ComponentModel.DataAnnotations;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tokenization.Domain.Exceptions;

namespace Tokenization.Api.ExceptionHandling.Config;

/// <summary>
/// DI registration for exception handling with enhanced security and sanitization.
/// </summary>
internal static partial class DependencyInjection
{
    /// <summary>
    /// Configures global exception handling to return sanitized ProblemDetails responses.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void UseTokenizationExceptionHandling(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;

                // Determine status code and sanitized response
                var (status, title, detail) = DetermineErrorResponse(ex, app.Environment.IsProduction());

                // Log the exception with appropriate level
                LogException(logger, ex, context, status);

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                    Type = GetProblemType(status),
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });
    }

    /// <summary>
    /// Determines the appropriate error response based on the exception and environment.
    /// </summary>
    /// <param name="ex">The exception</param>
    /// <param name="isProduction">Whether the application is running in production</param>
    /// <returns>A tuple containing status code, title, and detail</returns>
    private static (int Status, string Title, string? Detail) DetermineErrorResponse(Exception? ex, bool isProduction)
    {
        if (ex == null)
        {
            return (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null);
        }

        return ex switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                isProduction ? "One or more validation errors occurred." : SanitizeExceptionDetail(validationEx)
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Invalid argument.",
                isProduction ? "Invalid argument provided." : SanitizeExceptionDetail(argEx)
            ),
            SecurityException => (
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "You do not have permission to perform this action."
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized.",
                "Authentication is required to access this resource."
            ),
            TokenNotFoundException tokenEx => (
                StatusCodes.Status404NotFound,
                "Token not found.",
                isProduction ? "The requested token was not found." : SanitizeToken(tokenEx.Message)
            ),
            TokenInactiveException tokenEx => (
                StatusCodes.Status422UnprocessableEntity,
                "Token inactive.",
                isProduction ? "The requested token is inactive." : SanitizeToken(tokenEx.Message)
            ),
            TokenExpiredException tokenEx => (
                StatusCodes.Status422UnprocessableEntity,
                "Token expired.",
                isProduction ? "The requested token has expired." : SanitizeToken(tokenEx.Message)
            ),
            FluentValidation.ValidationException fluentEx => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation failed.",
                isProduction ? "One or more validation errors occurred." : SanitizeValidationException(fluentEx)
            ),
            InvalidOperationException invalidOpEx => (
                StatusCodes.Status422UnprocessableEntity,
                "Invalid operation.",
                isProduction ? "The requested operation is not valid." : SanitizeExceptionDetail(invalidOpEx)
            ),
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                "Request timeout.",
                "The request timed out. Please try again."
            ),
            TaskCanceledException => (
                StatusCodes.Status504GatewayTimeout,
                "Request cancelled.",
                "The request was cancelled. Please try again."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                isProduction ? "An internal server error occurred." : SanitizeExceptionDetail(ex)
            )
        };
    }

    /// <summary>
    /// Sanitizes exception details to prevent information disclosure.
    /// </summary>
    /// <param name="ex">The exception</param>
    /// <returns>Sanitized exception detail</returns>
    private static string SanitizeExceptionDetail(Exception ex)
    {
        var message = ex.Message;

        // Remove sensitive information patterns
        message = SensitivePattern2().Replace(message, "****-****-****-****");
        message = SensitivePattern3().Replace(message, "****-****-****-****");
        message = SensitivePattern4().Replace(message, "password=***");
        message = SensitivePattern5().Replace(message, "key=***");
        message = SensitivePattern6().Replace(message, "token=***");

        return message;
    }

    /// <summary>
    /// Sanitizes validation exception details.
    /// </summary>
    /// <param name="ex">The validation exception</param>
    /// <returns>Sanitized validation details</returns>
    private static string SanitizeValidationException(FluentValidation.ValidationException ex)
    {
        var errors = ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
        return string.Join("; ", errors);
    }

    /// <summary>
    /// Sanitizes token information in error messages.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>Sanitized message</returns>
    private static string SanitizeToken(string message)
    {
        return SensitivePattern1().Replace(message, "'****'");
    }

    /// <summary>
    /// Gets the appropriate problem type URI for the status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code</param>
    /// <returns>The problem type URI</returns>
    private static string GetProblemType(int statusCode) => statusCode switch
    {
        429 => "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
        _ => "https://datatracker.ietf.org/doc/html/rfc9457#section-3.2.1"
    };

    /// <summary>
    /// Logs the exception with appropriate level and context.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="ex">The exception</param>
    /// <param name="context">The HTTP context</param>
    /// <param name="statusCode">The HTTP status code</param>
    private static void LogException(ILogger logger, Exception? ex, HttpContext context, int statusCode)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userId = context.User.Identity?.Name ?? "anonymous";

        if (ex == null)
        {
            logger.LogError("Unhandled null exception for {Method} {Path} from {RemoteIp} by user {UserId}",
                context.Request.Method, context.Request.Path, remoteIp, userId);
            return;
        }

        var logLevel = statusCode >= 500 ? LogLevel.Error : LogLevel.Warning;

        logger.Log(logLevel, ex, "Exception occurred for {Method} {Path} from {RemoteIp} by user {UserId} with User-Agent: {UserAgent}",
            context.Request.Method, context.Request.Path, remoteIp, userId, userAgent);
    }

    [GeneratedRegex("'([^']{8,})'")]
    private static partial Regex SensitivePattern1();

    [GeneratedRegex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b")]
    private static partial Regex SensitivePattern2();

    [GeneratedRegex(@"\b\d{13,19}\b")]
    private static partial Regex SensitivePattern3();

    [GeneratedRegex(@"password[=:]\s*\S+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SensitivePattern4();

    [GeneratedRegex(@"key[=:]\s*\S+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SensitivePattern5();

    [GeneratedRegex(@"token[=:]\s*\S+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SensitivePattern6();
}
