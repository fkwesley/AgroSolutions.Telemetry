using Application.DTO.FieldMeasurement;
using Application.Mappings;
using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Application.Mappings
{
    public class FieldMeasurementMappingExtensionsTests
    {
        [Fact]
        public void ToEntity_ValidRequest_ShouldMapCorrectly()
        {
            // Arrange
            var request = new AddFieldMeasurementRequest
            {
                FieldId = Guid.NewGuid(),
                SoilMoisture = 65.5m,
                AirTemperature = 28.3m,
                Precipitation = 15.2m,
                CollectedAt = DateTime.UtcNow.AddHours(-2),
                UserId = "user-456"
            };

            // Act
            var entity = request.ToEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(request.FieldId, entity.FieldId);
            Assert.Equal(request.SoilMoisture, entity.SoilMoisture);
            Assert.Equal(request.AirTemperature, entity.AirTemperature);
            Assert.Equal(request.Precipitation, entity.Precipitation);
            Assert.Equal(request.CollectedAt, entity.CollectedAt);
            Assert.Equal(request.UserId, entity.UserId);
        }

        [Fact]
        public void ToResponse_ValidEntity_ShouldMapCorrectly()
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var collectedAt = DateTime.UtcNow.AddHours(-1);
            var userId = "user-789";

            var entity = new FieldMeasurement(
                fieldId,
                45.5m,
                22.3m,
                8.7m,
                collectedAt,
                userId);

            // Act
            var response = entity.ToResponse();

            // Assert
            Assert.Equal(entity.Id, response.Id);
            Assert.Equal(entity.FieldId, response.FieldId);
            Assert.Equal(entity.SoilMoisture, response.SoilMoisture);
            Assert.Equal(entity.AirTemperature, response.AirTemperature);
            Assert.Equal(entity.Precipitation, response.Precipitation);
            Assert.Equal(entity.CollectedAt, response.CollectedAt);
            Assert.Equal(entity.ReceivedAt, response.ReceivedAt);
            Assert.Equal(entity.UserId, response.UserId);
        }

        [Fact]
        public void ToEntity_ToResponse_RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var fieldId = Guid.NewGuid();
            var request = new AddFieldMeasurementRequest
            {
                FieldId = fieldId,
                SoilMoisture = 50m,
                AirTemperature = 25m,
                Precipitation = 10m,
                CollectedAt = DateTime.UtcNow.AddHours(-3),
                UserId = "user-round-trip"
            };

            // Act
            var entity = request.ToEntity();
            var response = entity.ToResponse();

            // Assert
            Assert.Equal(request.FieldId, response.FieldId);
            Assert.Equal(request.SoilMoisture, response.SoilMoisture);
            Assert.Equal(request.AirTemperature, response.AirTemperature);
            Assert.Equal(request.Precipitation, response.Precipitation);
            Assert.Equal(request.CollectedAt, response.CollectedAt);
        }
    }
}
