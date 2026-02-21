using Domain.Common;
using Domain.Exceptions;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Domain.Entities
{
    // #SOLID - Single Responsibility Principle (SRP)
    // A entidade FieldMeasurement é responsável APENAS por:
    // 1. Manter o estado das medições de campo
    // 2. Validar consistência dos dados
    // Ela NÃO é responsável por detecção de alertas, persistência ou comunicação externa.

    [DebuggerDisplay("Id: {Id}, FieldId: {FieldId}, SoilMoisture: {SoilMoisture}%, AirTemperature: {AirTemperature}°C")]
    public class FieldMeasurement : BaseEntity
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        public int FieldId { get; init; }
        public decimal SoilMoisture { get; init; }
        public decimal AirTemperature { get; init; }
        public decimal Precipitation { get; init; }
        public DateTime CollectedAt { get; init; }
        public DateTime ReceivedAt { get; init; }
        public string? UserId { get; init; }
        public string AlertEmailTo { get; init; }

        // Construtor público para criação via Application Layer
        public FieldMeasurement(
            int fieldId,
            decimal soilMoisture,
            decimal airTemperature,
            decimal precipitation,
            DateTime collectedAt,
            string alertEmailTo,
            string? userId = null)
        {
            Id = Guid.NewGuid();
            FieldId = fieldId;
            SoilMoisture = soilMoisture;
            AirTemperature = airTemperature;
            Precipitation = precipitation;
            CollectedAt = collectedAt;
            ReceivedAt = DateTime.UtcNow;
            UserId = userId;
            AlertEmailTo = alertEmailTo ?? throw new ArgumentNullException(nameof(alertEmailTo));

            // Validar dados
            Validate();
        }

        // #SOLID - Single Responsibility Principle (SRP)
        // Método de validação tem uma única responsabilidade: garantir a consistência dos dados
        private void Validate()
        {
            if (FieldId == 0)
                throw new BusinessException("FieldId não pode ser zero.");

            if (SoilMoisture < 0 || SoilMoisture > 100)
                throw new BusinessException("Umidade inválida. Deve estar entre 0 e 100.");

            if (AirTemperature < -50 || AirTemperature > 80)
                throw new BusinessException("Temperatura inválida. Deve estar entre -50 e 80.");

            if (Precipitation < 0)
                throw new BusinessException("Precipitação não pode ser negativa.");

            if (CollectedAt > DateTime.UtcNow)
                throw new BusinessException("Data de coleta não pode ser futura.");
        }
    }
}
