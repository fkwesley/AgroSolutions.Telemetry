using Domain.Common;
using Domain.Events;
using Domain.Exceptions;
using System.Diagnostics;

namespace Domain.Entities
{
    // #SOLID - Single Responsibility Principle (SRP)
    // A entidade FieldMeasurement é responsável por:
    // 1. Manter o estado das medições de campo
    // 2. Encapsular regras de negócio (validações de umidade, temperatura)
    // 3. Gerenciar eventos de domínio
    // Ela NÃO é responsável por persistência, logging ou comunicação externa.
    
    [DebuggerDisplay("Id: {Id}, FieldId: {FieldId}, SoilMoisture: {SoilMoisture}%, AirTemperature: {AirTemperature}°C, UserId: {UserId}")]
    public class FieldMeasurement : BaseEntity
    {
        public Guid Id { get; private set; }
        public Guid FieldId { get; private set; }
        public decimal SoilMoisture { get; private set; }
        public decimal AirTemperature { get; private set; }
        public decimal Precipitation { get; private set; }
        public DateTime CollectedAt { get; private set; }
        public DateTime ReceivedAt { get; private set; }
        public string? UserId { get; private set; }

        // EF Core constructor
        private FieldMeasurement() { }

        public FieldMeasurement(
            Guid fieldId,
            decimal soilMoisture,
            decimal airTemperature,
            decimal precipitation,
            DateTime collectedAt,
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

            Validate();
        }

        // #SOLID - Single Responsibility Principle (SRP)
        // Método de validação tem uma única responsabilidade: garantir a consistência dos dados
        private void Validate()
        {
            if (FieldId == Guid.Empty)
                throw new BusinessException("FieldId não pode ser vazio.");

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
