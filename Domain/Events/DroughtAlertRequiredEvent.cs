using System;

namespace Domain.Events
{
    /// <summary>
    /// Evento disparado quando é detectada uma condição de seca prolongada.
    /// Seca: umidade do solo < 30% por mais de 24 horas.
    /// </summary>
    public class DroughtAlertRequiredEvent : IDomainEvent
    {
        public int FieldId { get; }
        public decimal CurrentSoilMoisture { get; }
        public DateTime FirstLowMoistureDetected { get; }
        public DateTime OccurredOn { get; }

        public DroughtAlertRequiredEvent(
            int fieldId, 
            decimal currentSoilMoisture, 
            DateTime firstLowMoistureDetected)
        {
            FieldId = fieldId;
            CurrentSoilMoisture = currentSoilMoisture;
            FirstLowMoistureDetected = firstLowMoistureDetected;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
