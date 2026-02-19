using System.ComponentModel.DataAnnotations;

namespace Application.DTO.Alerts
{
    /// <summary>
    /// Standardized alert message payload for Service Bus.
    /// All field analysis handlers send alerts using this format.
    /// The consumer will use this to send email notifications.
    /// </summary>
    public class NotificationRequest
    {
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
        /// Email subject line
        /// </summary>
        [Required]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body with detailed explanation of the alert
        /// Should include:
        /// - What was evaluated
        /// - Current metrics
        /// - Why this is important
        /// - Recommended actions (if applicable)
        /// </summary>
        [Required]
        public string Body { get; set; } = string.Empty;

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
        public string AlertType { get; set; } = string.Empty;
        public int FieldId { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    }
}
