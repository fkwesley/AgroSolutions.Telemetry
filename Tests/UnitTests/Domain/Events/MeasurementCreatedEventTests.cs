using Xunit;
using FieldMeasurement = global::Domain.Entities.FieldMeasurement;
using MeasurementCreatedEvent = global::Domain.Events.MeasurementCreatedEvent;

namespace Tests.UnitTests.Domain.Events
{
    public class MeasurementCreatedEventTests
    {
        [Fact]
        public void Constructor_WithValidMeasurement_ShouldCreateEvent()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, 25, 10, DateTime.UtcNow, "alerts@farm.com");

            // Act
            var domainEvent = new MeasurementCreatedEvent(measurement);

            // Assert
            Assert.NotNull(domainEvent);
            Assert.Equal(measurement, domainEvent.Measurement);
            Assert.True(domainEvent.OccurredOn <= DateTime.UtcNow);
            Assert.True(domainEvent.OccurredOn >= DateTime.UtcNow.AddSeconds(-1));
        }

        [Fact]
        public void Constructor_WithNullMeasurement_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MeasurementCreatedEvent(null!));
        }

        [Fact]
        public void MeasurementCreatedEvent_ShouldCarryCompleteMeasurement()
        {
            // Arrange
            var fieldId = 123;
            var soilMoisture = 45.5m;
            var airTemperature = 28.7m;
            var precipitation = 15.2m;
            var collectedAt = DateTime.UtcNow.AddHours(-1);
            var userId = "user-456";

            var measurement = new FieldMeasurement(
                fieldId, soilMoisture, airTemperature, precipitation, collectedAt, "alerts@farm.com", userId);

            // Act
            var domainEvent = new MeasurementCreatedEvent(measurement);

            // Assert - Event should carry complete measurement data
            Assert.Equal(fieldId, domainEvent.Measurement.FieldId);
            Assert.Equal(soilMoisture, domainEvent.Measurement.SoilMoisture);
            Assert.Equal(airTemperature, domainEvent.Measurement.AirTemperature);
            Assert.Equal(precipitation, domainEvent.Measurement.Precipitation);
            Assert.Equal(collectedAt, domainEvent.Measurement.CollectedAt);
            Assert.Equal(userId, domainEvent.Measurement.UserId);
        }
    }
}
