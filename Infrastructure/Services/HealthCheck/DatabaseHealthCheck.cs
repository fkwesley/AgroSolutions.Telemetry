using Application.DTO.Health;
using Application.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.Services.HealthCheck
{
    /// <summary>
    /// Database Health Check Implementation
    /// #SOLID - Single Responsibility Principle (SRP)
    /// Esta classe tem uma ÚNICA responsabilidade: verificar a saúde do banco de dados.
    /// 
    /// #SOLID - Open/Closed Principle (OCP)
    /// Implementa IHealthCheck - pode ser adicionada sem modificar HealthCheckService
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly OrdersDbContext _dbContext;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public string ComponentName => "Database";
        public bool IsCritical => true; // Database é CRÍTICO - se falhar, API retorna 503

        public DatabaseHealthCheck(
            OrdersDbContext dbContext,
            ILogger<DatabaseHealthCheck> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentHealth> CheckHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                stopwatch.Stop();

                if (canConnect)
                {
                    return new ComponentHealth
                    {
                        Status = "Healthy",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Description = "Database connection successful"
                    };
                }

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = "Cannot connect to database"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Database health check failed");

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = $"Database error: {ex.Message}"
                };
            }
        }
    }
}



