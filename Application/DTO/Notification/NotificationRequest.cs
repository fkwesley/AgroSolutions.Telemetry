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
    /// <summary>
    /// Enum para prioridade da notificação.
    /// </summary>
    public enum PriorityEnum
    {
        Low,
        Normal,
        High,
        Urgent
    }

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
        /// priority level (Low, Normal, High, Urgent).
        /// Maps to SMTP headers (X-Priority, Importance) for email client display.
        /// Default: Normal
        /// </summary>
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public PriorityEnum Priority { get; set; } = PriorityEnum.Normal;

    }
}
