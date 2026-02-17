using API.Helpers;
using API.Models;
using Application.DTO.Health;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers.v1
{
    /// <summary>
    /// Health Check Controller V1
    /// </summary>
    [ApiController]
    [Route("v{version:apiVersion}/health")]
    [ApiVersion("1.0")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

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
        /// health check with all dependencies 
        /// </summary>
        /// <returns>Health status with all component details</returns>
        /// <response code="200">API is healthy or degraded</response>
        /// <response code="503">API is unhealthy (critical components failed)</response>
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        [HttpGet(Name = "GetHealth_V1")]
        public async Task<IActionResult> GetHealth()
        {
            // Get correlation ID from context (set by CorrelationIdMiddleware)
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() 
                ?? HttpContext.TraceIdentifier;

            _logger.LogInformation("Health check requested with CorrelationId: {CorrelationId}", correlationId);

            var healthStatus = await _healthCheckService.CheckHealthAsync();

            if (healthStatus == null)
            {
                _logger.LogError("Health check service returned null - CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ErrorResponse { Message = "Health check failed - null response" });
            }

            _logger.LogInformation(
                "Health check completed: Status={Status}, Components={ComponentCount}, CorrelationId={CorrelationId}", 
                healthStatus.Status, 
                healthStatus.Components?.Count ?? 0,
                correlationId);

            // Add HATEOAS links
            healthStatus.Links = HateoasHelper.GenerateHealthLinks(HttpContext, "1.0");

            // Return 503 if unhealthy (critical components failed)
            if (healthStatus.Status == "Unhealthy")
            {
                _logger.LogWarning("Returning 503 - API is unhealthy - CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, healthStatus);
            }

            return Ok(healthStatus);
        }
    }
}

