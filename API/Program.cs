using API.Configurations;
using Serilog;

// ===========================
// SERILOG CONFIGURATION
// ===========================
// Configura Serilog ANTES de construir o WebApplication
// Isso permite capturar logs durante o startup da aplicação
//
// Ordem de prioridade dos Configuration Providers (último vence):
//   1. appsettings.json
//   2. appsettings.{Environment}.json
//   3. Environment Variables
//   4. Azure Key Vault (maior prioridade - sobrescreve todas as anteriores)

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .AddKeyVaultConfiguration()
    .Build();

LoggingConfiguration.ConfigureSerilog(configuration);

try
{
    Log.Information("Starting Telemetry.API");

    // Loga a origem de cada configuração para diagnóstico
    ConfigurationSourceLogger.LogConfigurationSources(configuration);

    var builder = WebApplication.CreateBuilder(args);

    // Replica a mesma cadeia de configuração no builder do WebApplication
    // para garantir que todos os serviços usem a mesma prioridade
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables()
        .AddKeyVaultConfiguration();

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