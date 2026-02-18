using Domain.Common;
using Domain.Entities;

namespace Domain.Events
{
    /// <summary>
    /// Domain event raised when a new field measurement is created.
    /// Carries the complete measurement object to avoid additional database queries.
    /// </summary>
    public class MeasurementCreatedEvent : IDomainEvent
    {
        /// <summary>
        /// Complete field measurement object
        /// </summary>
        public FieldMeasurement Measurement { get; }

        /// <summary>
        /// When the event occurred
        /// </summary>
        public DateTime OccurredOn { get; }

        public MeasurementCreatedEvent(FieldMeasurement measurement)
        {
            Measurement = measurement ?? throw new ArgumentNullException(nameof(measurement));
            OccurredOn = DateTime.UtcNow;
        }
    }
}
