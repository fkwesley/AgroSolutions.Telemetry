using Infrastructure.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tests.IntegrationTests.Common;

/// <summary>
/// Custom WebApplicationFactory para testes de integração.
/// Configura um ambiente de teste com banco de dados in-memory.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove o DbContext real
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrdersDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Adiciona DbContext usando InMemory database
            services.AddDbContext<OrdersDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Build ServiceProvider
            var serviceProvider = services.BuildServiceProvider();

            // Cria escopo para inicializar o banco
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<OrdersDbContext>();

            // Garante que o banco está criado
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}
