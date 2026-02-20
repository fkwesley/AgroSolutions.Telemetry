using System.ComponentModel.DataAnnotations;

namespace Application.DTO.Alerts
{
    /// <summary>
    /// Template-based notification request for Service Bus.
    /// The consumer (notification service) will:
    /// 1. Load the template based on TemplateId
    /// 2. Replace placeholders in the template with values from Parameters dictionary
    /// 3. Send the email with the rendered content
    /// </summary>
    public class NotificationRequest
    {
        /// <summary>
        /// Template identifier (e.g., "Drought", "ExcessiveRainfall", "Irrigation")
        /// The notification service will use this to load the appropriate email template.
        /// </summary>
        [Required]
        public string TemplateId { get; set; } = string.Empty;

        /// <summary>
        /// Email recipients (primary)
        /// </summary>
        [Required]
        public List<string> EmailTo { get; set; } = new();

        /// <summary>
        /// Email carbon copy recipients
        /// </summary>
        public List<string> EmailCc { get; set; } = new();

        /// <summary>
        /// Email blind carbon copy recipients
        /// </summary>
        public List<string> EmailBcc { get; set; } = new();

        /// <summary>
        /// Template parameters for placeholder replacement.
        /// Key: Placeholder name (e.g., "{fieldId}", "{soilMoisture}")
        /// Value: Actual value to replace the placeholder with
        /// 
        /// Example:
        /// {
        ///   "{fieldId}": "42",
        ///   "{detectedAt}": "2026-02-19 15:30:00 UTC",
        ///   "{soilMoisture}": "15.5"
        /// }
        /// </summary>
        [Required]
        public Dictionary<string, string> Parameters { get; set; } = new();

        /// <summary>
        /// Alert metadata for tracking and categorization
        /// </summary>
        public AlertMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metadata for alert tracking and categorization
    /// </summary>
    public class AlertMetadata
    {
        /// <summary>
        /// Unique identifier for correlating this alert across systems and logs.
        /// Should be the same CorrelationId from the original request/measurement.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        public string AlertType { get; set; } = string.Empty;
        public int FieldId { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    }
}
