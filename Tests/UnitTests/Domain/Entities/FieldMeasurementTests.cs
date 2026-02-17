using Domain.Entities;
using Domain.Exceptions;
using Xunit;

namespace Tests.UnitTests.Domain.Entities
{
    public class FieldMeasurementTests
    {
        [Fact]
        public void Constructor_ValidData_ShouldCreateMeasurement()
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var soilMoisture = 45.5m;
            var airTemperature = 25.3m;
            var precipitation = 12.7m;
            var collectedAt = DateTime.UtcNow.AddHours(-1);
            var userId = "user-123";

            // Act
            var measurement = new FieldMeasurement(
                fieldId,
                soilMoisture,
                airTemperature,
                precipitation,
                collectedAt,
                userId);

            // Assert
            Assert.NotEqual(Guid.Empty, measurement.Id);
            Assert.Equal(fieldId, measurement.FieldId);
            Assert.Equal(soilMoisture, measurement.SoilMoisture);
            Assert.Equal(airTemperature, measurement.AirTemperature);
            Assert.Equal(precipitation, measurement.Precipitation);
            Assert.Equal(collectedAt, measurement.CollectedAt);
            Assert.Equal(userId, measurement.UserId);
            Assert.True(measurement.ReceivedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithoutUserId_ShouldCreateMeasurement()
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var soilMoisture = 45.5m;
            var airTemperature = 25.3m;
            var precipitation = 12.7m;
            var collectedAt = DateTime.UtcNow.AddHours(-1);

            // Act
            var measurement = new FieldMeasurement(
                fieldId,
                soilMoisture,
                airTemperature,
                precipitation,
                collectedAt);

            // Assert
            Assert.NotEqual(Guid.Empty, measurement.Id);
            Assert.Equal(fieldId, measurement.FieldId);
            Assert.Null(measurement.UserId);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Constructor_InvalidSoilMoisture_ShouldThrowBusinessException(decimal invalidMoisture)
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var collectedAt = DateTime.UtcNow;

            // Act & Assert
            var exception = Assert.Throws<BusinessException>(() =>
                new FieldMeasurement(fieldId, invalidMoisture, 25, 10, collectedAt));

            Assert.Contains("Umidade inválida", exception.Message);
        }

        [Theory]
        [InlineData(-51)]
        [InlineData(81)]
        public void Constructor_InvalidAirTemperature_ShouldThrowBusinessException(decimal invalidTemp)
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var collectedAt = DateTime.UtcNow;

            // Act & Assert
            var exception = Assert.Throws<BusinessException>(() =>
                new FieldMeasurement(fieldId, 50, invalidTemp, 10, collectedAt));

            Assert.Contains("Temperatura inválida", exception.Message);
        }

        [Fact]
        public void Constructor_NegativePrecipitation_ShouldThrowBusinessException()
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var collectedAt = DateTime.UtcNow;

            // Act & Assert
            var exception = Assert.Throws<BusinessException>(() =>
                new FieldMeasurement(fieldId, 50, 25, -5, collectedAt));

            Assert.Contains("Precipitação não pode ser negativa", exception.Message);
        }

        [Fact]
        public void Constructor_FutureCollectedDate_ShouldThrowBusinessException()
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var futureDate = DateTime.UtcNow.AddHours(1);

            // Act & Assert
            var exception = Assert.Throws<BusinessException>(() =>
                new FieldMeasurement(fieldId, 50, 25, 10, futureDate));

            Assert.Contains("Data de coleta não pode ser futura", exception.Message);
        }

        [Fact]
        public void Constructor_EmptyFieldId_ShouldThrowBusinessException()
        {
            // Arrange
            var collectedAt = DateTime.UtcNow;

            // Act & Assert
            var exception = Assert.Throws<BusinessException>(() =>
                new FieldMeasurement(Guid.Empty, 50, 25, 10, collectedAt));

            Assert.Contains("FieldId não pode ser vazio", exception.Message);
        }

        [Fact]
        public void Constructor_BoundaryValues_ShouldSucceed()
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var collectedAt = DateTime.UtcNow;

            // Act - Testing boundary values
            var measurement1 = new FieldMeasurement(fieldId, 0, -50, 0, collectedAt);
            var measurement2 = new FieldMeasurement(fieldId, 100, 80, 1000, collectedAt);

            // Assert
            Assert.NotNull(measurement1);
            Assert.NotNull(measurement2);
        }
    }
}
