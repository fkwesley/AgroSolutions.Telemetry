using Application.DTO.Health;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.HealthCheck
{
    /// <summary>
    /// Health check para Azure Service Bus.
    /// Verifica conectividade com o namespace do Service Bus.
    /// </summary>
    public class ServiceBusHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceBusHealthCheck> _logger;

        public string ComponentName => "ServiceBus";
        public bool IsCritical => false; // Não é crítico - API funciona sem Service Bus

        public ServiceBusHealthCheck(
            IConfiguration configuration,
            ILogger<ServiceBusHealthCheck> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentHealth> CheckHealthAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var connectionString = _configuration.GetConnectionString("ServiceBusConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return new ComponentHealth
                    {
                        Status = "Degraded",
                        Description = "Service Bus connection string not configured",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                await using var client = new ServiceBusClient(connectionString);

                // Tenta criar um sender para verificar conectividade
                var queueName = _configuration["AlertSettings:DroughtAlertQueueName"] ?? "alert-required-queue";
                await using var sender = client.CreateSender(queueName);

                stopwatch.Stop();

                var status = stopwatch.ElapsedMilliseconds switch
                {
                    < 100 => "Healthy",
                    < 500 => "Degraded",
                    _ => "Unhealthy"
                };

                return new ComponentHealth
                {
                    Status = status,
                    Description = $"Service Bus queue '{queueName}' accessible. Namespace: {client.FullyQualifiedNamespace}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (ServiceBusException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Service Bus health check failed");

                return new ComponentHealth
                {
                    Status = "Degraded",
                    Description = $"Service Bus error: {ex.Reason} - {ex.Message} (Transient: {ex.IsTransient})",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Service Bus health check failed with unexpected error");

                return new ComponentHealth
                {
                    Status = "Degraded",
                    Description = $"Service Bus connection failed: {ex.Message}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }
    }
}
