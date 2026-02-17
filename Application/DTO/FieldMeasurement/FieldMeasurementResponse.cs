using Application.DTO.Common;

namespace Application.DTO.FieldMeasurement
{
    /// <summary>
    /// Response DTO for field measurements.
    /// </summary>
    public class FieldMeasurementResponse : IHateoasResource
    {
        public Guid Id { get; set; }
        public int FieldId { get; set; }
        public decimal SoilMoisture { get; set; }
        public decimal AirTemperature { get; set; }
        public decimal Precipitation { get; set; }
        public DateTime CollectedAt { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string? UserId { get; set; }
        public List<Link> Links { get; set; } = new();
    }
}
