using System.ComponentModel.DataAnnotations;

namespace Application.DTO.FieldMeasurement
{
    /// <summary>
    /// DTO para criação de nova medição de campo.
    /// </summary>
    public class AddFieldMeasurementRequest
    {
        [Required(ErrorMessage = "FieldId é obrigatório.")]
        public Guid FieldId { get; set; }

        [Required(ErrorMessage = "Umidade do solo é obrigatória.")]
        [Range(0, 100, ErrorMessage = "Umidade deve estar entre 0 e 100.")]
        public decimal SoilMoisture { get; set; }

        [Required(ErrorMessage = "Temperatura do ar é obrigatória.")]
        [Range(-50, 80, ErrorMessage = "Temperatura deve estar entre -50 e 80.")]
        public decimal AirTemperature { get; set; }

        [Required(ErrorMessage = "Precipitação é obrigatória.")]
        [Range(0, double.MaxValue, ErrorMessage = "Precipitação não pode ser negativa.")]
        public decimal Precipitation { get; set; }

        [Required(ErrorMessage = "Data de coleta é obrigatória.")]
        public DateTime CollectedAt { get; set; }
        public string? UserId { get; set; }
    }
}
