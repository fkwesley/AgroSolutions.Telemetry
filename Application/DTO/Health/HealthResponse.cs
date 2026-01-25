using Application.DTO.Common;

namespace Application.DTO.Health
{
    /// <summary>
    /// Health Check Response with detailed status information
    /// </summary>
    public class HealthResponse : IHateoasResource
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Application version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Individual component health statuses
        /// </summary>
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();

        /// <summary>
        /// HATEOAS links
        /// </summary>
        public List<Link> Links { get; set; } = new();
    }

    /// <summary>
    /// Individual component health status
    /// </summary>
    public class ComponentHealth
    {
        /// <summary>
        /// Component status (Healthy, Degraded, Unhealthy)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public long? ResponseTimeMs { get; set; }

        /// <summary>
        /// Additional details or error message
        /// </summary>
        public string? Description { get; set; }
    }
}
