using API.Helpers;
using API.Models;
using Application.DTO.Health;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.v2
{
    /// <summary>
    /// Health Check Controller V2
    /// Single comprehensive endpoint that checks all external dependencies
    /// </summary>
    [ApiController]
    [Route("v{version:apiVersion}/health")]
    [ApiVersion("2.0")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        // #SOLID - Dependency Inversion Principle (DIP)
        public HealthController(
            IHealthCheckService healthCheckService,
            ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService 
                ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger 
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Comprehensive health check with all external dependencies
        /// </summary>
        /// <remarks>
        /// Checks the following components:
        /// - **Database**: SQL Server connectivity and response time
        /// - **GamesAPI**: External Games API availability
        /// - **RabbitMQ**: Message broker connectivity (if enabled)
        /// - **Elasticsearch**: Logging service availability
        /// - **System**: Memory usage and system resources
        /// 
        /// Status Levels:
        /// - **Healthy**: All components operational
        /// - **Degraded**: Non-critical components unavailable (API still functional)
        /// - **Unhealthy**: Critical components failed (Database)
        /// 
        /// </remarks>
        /// <returns>Comprehensive health status with all dependency details</returns>
        /// <response code="200">API is healthy or degraded (non-critical issues)</response>
        /// <response code="503">API is unhealthy (critical component failed, e.g., Database)</response>
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        [HttpGet(Name = "GetHealth_V2")]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var healthStatus = await _healthCheckService.CheckHealthAsync();

                // Add HATEOAS links
                healthStatus.Links = HateoasHelper.GenerateHealthLinks(HttpContext, "2.0");

                // Log health status summary
                var componentsSummary = string.Join(", ", 
                    healthStatus.Components.Select(c => $"{c.Key}={c.Value.Status}"));

                _logger.LogInformation(
                    "Health check completed. Overall: {Status}, Components: [{Components}]",
                    healthStatus.Status,
                    componentsSummary);

                // Return 503 only if CRITICAL components (Database) are unhealthy
                // Return 200 if degraded (non-critical services down, API still works)
                if (healthStatus.Status == "Unhealthy")
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, healthStatus);

                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed with exception");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "Health check failed",
                    Detail = ex.Message
                });
            }
        }
    }
}

