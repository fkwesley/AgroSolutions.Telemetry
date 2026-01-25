using API.Middlewares;

namespace API.Configurations;

public static class MiddlewareConfiguration
{
    public static WebApplication UseCustomMiddlewares(this WebApplication app)
    {
        app.UseHttpsRedirection();
        
        // Security Headers - Deve vir cedo no pipeline
        app.UseMiddleware<SecurityHeadersMiddleware>();
        
        // Cache Headers
        app.UseMiddleware<NoCacheMiddleware>();
        
        app.UseMiddleware<ApiVersionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ErrorHandlingMiddleware>();

        return app;
    }
}
