using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTO.FieldMeasurement
{
    /// <summary>
    /// DTO for creating a new field measurement.
    /// </summary>
    public class AddFieldMeasurementRequest
    {
        [Required(ErrorMessage = "FieldId is required.")]
        public int FieldId { get; set; }

        [Required(ErrorMessage = "Soil moisture is required.")]
        [Range(0, 100, ErrorMessage = "Soil moisture must be between 0 and 100.")]
        public decimal SoilMoisture { get; set; }

        [Required(ErrorMessage = "Air temperature is required.")]
        [Range(-50, 80, ErrorMessage = "Air temperature must be between -50 and 80.")]
        public decimal AirTemperature { get; set; }

        [Required(ErrorMessage = "Precipitation is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Precipitation cannot be negative.")]
        public decimal Precipitation { get; set; }

        [Required(ErrorMessage = "Collection date is required.")]
        public DateTime CollectedAt { get; set; }

        /// <summary>
        /// Email para receber alertas e notificações relacionadas a esta medição.
        /// </summary>
        [Required(ErrorMessage = "Alert email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string AlertEmailTo { get; set; } = string.Empty;

        [JsonIgnore]
        public string? UserId { get; set; }
    }
}
