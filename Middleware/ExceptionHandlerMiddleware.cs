using System.Net;
using System.Text.Json;

namespace SmartProManWebAPI.Middleware
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = ex switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access."),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(new
            {
                statusCode = (int)statusCode,
                message,
                // Only show stack trace in development
                detail = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? ex.StackTrace
                    : null
            });

            return context.Response.WriteAsync(result);
        }
    }

    // Extension method for clean registration in Program.cs
    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
