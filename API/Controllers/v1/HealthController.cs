using API.Helpers;
using API.Models;
using Application.DTO.Health;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.v1
{
    /// <summary>
    /// Health Check Controller V1 (⚠️ DEPRECATED - Use V2)
    /// </summary>
    [ApiController]
    [Route("v{version:apiVersion}/health")]
    [ApiVersion("1.0", Deprecated = true)]
    public class HealthController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthController(IHealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService 
                ?? throw new ArgumentNullException(nameof(healthCheckService));
        }

        /// <summary>
        /// Comprehensive health check with all dependencies (Database, Games API, RabbitMQ, Elasticsearch, System)
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
            var healthStatus = await _healthCheckService.CheckHealthAsync();

            // Add HATEOAS links
            healthStatus.Links = HateoasHelper.GenerateHealthLinks(HttpContext, "1.0");

            // Return 503 if unhealthy (critical components failed)
            if (healthStatus.Status == "Unhealthy")
                return StatusCode(StatusCodes.Status503ServiceUnavailable, healthStatus);

            return Ok(healthStatus);
        }
    }
}

