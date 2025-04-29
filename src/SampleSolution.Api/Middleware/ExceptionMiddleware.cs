// -------------------------------------------------------------------------------------
//  <copyright file="ExceptionMiddleware.cs" company="{Company Name}">
//    Copyright (c) {Company Name}. All rights reserved.
//  </copyright>
// -------------------------------------------------------------------------------------

namespace SampleSolution.Api.Middleware;

using System.Net;
using System.Text.Json;

/// <summary>
/// Middleware for handling exceptions globally
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">The logger</param>
    /// <param name="env">The web host environment</param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)GetStatusCode(exception);

        var response = new
        {
            statusCode = context.Response.StatusCode,
            message = GetMessageForResponse(exception),
            // Only include detailed error info in development
            detail = _env.IsDevelopment() ? exception.StackTrace : null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }

    private HttpStatusCode GetStatusCode(Exception exception)
    {
        // Map different exception types to appropriate HTTP status codes
        return exception switch
        {
            FileNotFoundException => HttpStatusCode.NotFound,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            // Add more exception types as needed
            _ => HttpStatusCode.InternalServerError
        };
    }

    private string GetMessageForResponse(Exception exception)
    {
        // Customize the error message based on exception type
        return exception switch
        {
            FileNotFoundException => "The requested resource was not found.",
            UnauthorizedAccessException => "You are not authorized to access this resource.",
            // For security reasons, we might want to provide generic messages for certain exceptions in production
            _ => _env.IsDevelopment() ? exception.Message : "An unexpected error occurred."
        };
    }
}

/// <summary>
/// Extension methods for the Exception Middleware
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    /// <summary>
    /// Configures the application to use the custom exception middleware
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}