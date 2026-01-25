using API.Configurations;
using Serilog;

// ===========================
// SERILOG CONFIGURATION
// ===========================
// Configura Serilog ANTES de construir o WebApplication
// Isso permite capturar logs durante o startup da aplicação

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

LoggingConfiguration.ConfigureSerilog(configuration);

try
{
    Log.Information("Starting Orders.API");

    var builder = WebApplication.CreateBuilder(args);

    // Adiciona Serilog como provider de logging do ASP.NET Core
    builder.Host.UseSerilog();

    // Service configurations
    builder.AddAuthenticationConfiguration();
    builder.AddVersioningConfiguration();
    builder.AddSwaggerConfiguration();
    builder.AddCorsConfiguration();
    builder.AddDependencyInjection();
    builder.AddValidationConfiguration();

    var app = builder.Build();

    // Middleware pipeline
    app.UseSwaggerConfiguration();
    app.UseCorsConfiguration();
    app.UseCustomMiddlewares();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class visible to IntegrationTests
public partial class Program { }