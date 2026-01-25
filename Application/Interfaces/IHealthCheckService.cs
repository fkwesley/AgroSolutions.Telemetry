using Application.DTO.Health;

namespace Application.Interfaces
{
    /// <summary>
    /// Health Check Service Interface
    /// Follows SOLID - Dependency Inversion Principle (DIP)
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Performs comprehensive health check of all system components
        /// </summary>
        /// <returns>Health check response with detailed status</returns>
        Task<HealthResponse> CheckHealthAsync();
    }
}
