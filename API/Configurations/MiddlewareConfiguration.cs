using API.Middlewares;

namespace API.Configurations;

public static class MiddlewareConfiguration
{
    public static WebApplication UseCustomMiddlewares(this WebApplication app)
    {
        app.UseHttpsRedirection();

        // Security Headers - Must come early in pipeline
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Cache Headers
        app.UseMiddleware<NoCacheMiddleware>();

        app.UseMiddleware<ApiVersionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Request Logging - Handles CorrelationId, logs requests/responses
        // After authentication to capture user info
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Error Handling - Last to catch all errors
        app.UseMiddleware<ErrorHandlingMiddleware>();

        return app;
    }
}
