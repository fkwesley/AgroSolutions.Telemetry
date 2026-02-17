using Application.DTO.Health;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Application.Services
{
    // #SOLID - Single Responsibility Principle (SRP)
    // Esta classe tem uma única responsabilidade: orquestrar verificações de saúde do sistema.
    
    // #SOLID - Open/Closed Principle (OCP)
    // ABERTA para extensão: Novos health checks são descobertos automaticamente via DI
    // FECHADA para modificação: Não precisa editar esta classe para adicionar novo check
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Depende de abstrações (IHealthCheck) injetadas via construtor.
    // Usa IEnumerable<IHealthCheck> para auto-discovery
    public class HealthCheckService : IHealthCheckService
    {
        private readonly IEnumerable<IHealthCheck> _healthChecks;
        private readonly ILogger<HealthCheckService> _logger;
        private readonly string _version;

        public HealthCheckService(
            IEnumerable<IHealthCheck> healthChecks,
            ILogger<HealthCheckService> logger)
        {
            _healthChecks = healthChecks ?? throw new ArgumentNullException(nameof(healthChecks));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "1.0.0";
        }

        /// <summary>
        /// Comprehensive health check of all components and external dependencies
        /// Descobre e executa TODOS os health checks registrados automaticamente
        /// </summary>
        public async Task<HealthResponse> CheckHealthAsync()
        {
            _logger.LogInformation("Performing comprehensive health check");

            var response = new HealthResponse
            {
                Timestamp = DateTime.UtcNow,
                Version = _version,
                Components = new Dictionary<string, ComponentHealth>()
            };

            // #OCP - Descobre automaticamente todos os IHealthCheck registrados no DI
            // Novo health check? Só precisa implementar IHealthCheck e registrar no DI!

            _logger.LogInformation("Starting health checks for {Count} components", _healthChecks.Count());

            var tasks = _healthChecks
                .Select(async check =>
                {
                    try
                    {
                        _logger.LogInformation("Checking health of component: {ComponentName}", check.ComponentName);
                        var result = await check.CheckHealthAsync();
                        _logger.LogInformation(
                            "Health check completed for {ComponentName}: Status={Status}, ResponseTime={ResponseTime}ms",
                            check.ComponentName,
                            result.Status,
                            result.ResponseTimeMs);
                        return new { check.ComponentName, Health = result };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Health check FAILED for component: {ComponentName} - {ErrorMessage}", 
                            check.ComponentName, 
                            ex.Message);
                        return new
                        {
                            check.ComponentName,
                            Health = new ComponentHealth
                            {
                                Status = "Unhealthy",
                                Description = $"Health check threw exception: {ex.Message}"
                            }
                        };
                    }
                })
                .ToList();

            // Wait for all checks to complete
            var results = await Task.WhenAll(tasks);

            // Collect results
            foreach (var result in results)
                response.Components[result.ComponentName] = result.Health;

            // Determine overall status
            response.Status = DetermineOverallStatus(response.Components);

            _logger.LogInformation(
                "Health check completed with status: {Status}. Components checked: {ComponentCount}",
                response.Status,
                response.Components.Count);

            return response;
        }

        /// <summary>
        /// Determine overall status based on component statuses and criticality
        /// 
        /// Status Hierarchy (worst to best):
        /// Unhealthy   -> Critical component failed (Database) -> API should NOT receive traffic
        /// Degraded    -> Non-critical component failed        -> API works with reduced functionality
        /// Healthy     -> All components operational
        /// </summary>
        private string DetermineOverallStatus(Dictionary<string, ComponentHealth> components)
        {
            if (!components.Any())
                return "Unknown";

            // #CRITICAL: Se algum componente CRÍTICO estiver Unhealthy -> Status geral = Unhealthy
            // Componentes críticos são identificados pela propriedade IsCritical
            var criticalChecks = _healthChecks.Where(c => c.IsCritical).Select(c => c.ComponentName);
            
            if (criticalChecks.Any(componentName => 
                components.ContainsKey(componentName) && 
                components[componentName].Status == "Unhealthy"))
            {
                return "Unhealthy";  // Return 503, remove from load balancer
            }

            // #NON-CRITICAL: Se algum componente (crítico ou não) estiver Unhealthy -> Degraded
            if (components.Values.Any(c => c.Status == "Unhealthy"))
                return "Degraded";  // Return 200, keep in load balancer

            // #DEGRADED: Qualquer componente com performance degradada
            if (components.Values.Any(c => c.Status == "Degraded"))
                return "Degraded";

            // #HEALTHY: Todos os sistemas operacionais
            return "Healthy";  
        }
    }
}


