using Application.DTO.Health;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.HealthCheck
{
    /// <summary>
    /// System (Memory/CPU) Health Check Implementation
    /// #SOLID - Single Responsibility Principle (SRP)
    /// Verifica os recursos do sistema (memória)
    /// </summary>
    public class SystemHealthCheck : IHealthCheck
    {
        private readonly ILogger<SystemHealthCheck> _logger;

        public string ComponentName => "System";
        public bool IsCritical => false; // Não-crítico - apenas informativo

        public SystemHealthCheck(ILogger<SystemHealthCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ComponentHealth> CheckHealthAsync()
        {
            try
            {
                var memoryUsedMb = GC.GetTotalMemory(false) / 1024 / 1024;
                
                // Thresholds: < 500 MB = Healthy, 500-1000 MB = Degraded, > 1000 MB = Unhealthy
                var status = memoryUsedMb < 500 
                    ? "Healthy" 
                    : memoryUsedMb < 1000 
                        ? "Degraded" 
                        : "Unhealthy";

                return Task.FromResult(new ComponentHealth
                {
                    Status = status,
                    Description = $"Memory: {memoryUsedMb} MB"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "System health check failed");

                return Task.FromResult(new ComponentHealth
                {
                    Status = "Unknown",
                    Description = "Failed to retrieve system info"
                });
            }
        }

    }
}
