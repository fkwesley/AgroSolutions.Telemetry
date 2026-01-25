using Application.DTO.Health;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Diagnostics;

namespace Infrastructure.Services.HealthCheck
{
    /// <summary>
    /// RabbitMQ Health Check Implementation
    /// #SOLID - Single Responsibility Principle (SRP)
    /// Verifica a conectividade com RabbitMQ usando a mesma lógica de conexão do publisher
    /// </summary>
    public class RabbitMQHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQHealthCheck> _logger;

        public string ComponentName => "RabbitMQ";
        public bool IsCritical => false; // Não-crítico - API funciona sem RabbitMQ

        public RabbitMQHealthCheck(
            IConfiguration configuration,
            ILogger<RabbitMQHealthCheck> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentHealth> CheckHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var connectionString = _configuration.GetConnectionString("FCGRabbitMQConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    return new ComponentHealth
                    {
                        Status = "Unknown",
                        Description = "RabbitMQ connection string not configured"
                    };
                }

                // Tenta criar uma conexão real para verificar
                var factory = new ConnectionFactory 
                { 
                    Uri = new Uri(connectionString),
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                stopwatch.Stop();
                return new ComponentHealth
                {
                    Status = "Healthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = "RabbitMQ connection verified"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "RabbitMQ health check failed");

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = $"RabbitMQ error: {ex.Message}"
                };
            }
        }
    }
}
