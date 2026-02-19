using Application.EventHandlers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FieldMeasurement = global::Domain.Entities.FieldMeasurement;
using MeasurementCreatedEvent = global::Domain.Events.MeasurementCreatedEvent;

namespace Tests.UnitTests.Application.EventHandlers
{
    public class FreezingTemperatureAnalysisEventHandlerTests
    {
        private readonly Mock<IMessagePublisherFactory> _publisherFactoryMock;
        private readonly Mock<IMessagePublisher> _publisherMock;
        private readonly Mock<ILogger<FreezingTemperatureAnalysisEventHandler>> _loggerMock;
        private readonly global::Application.Settings.FreezingTemperatureSettings _settings;
        private readonly FreezingTemperatureAnalysisEventHandler _handler;

        public FreezingTemperatureAnalysisEventHandlerTests()
        {
            _publisherFactoryMock = new Mock<IMessagePublisherFactory>();
            _publisherMock = new Mock<IMessagePublisher>();
            _loggerMock = new Mock<ILogger<FreezingTemperatureAnalysisEventHandler>>();
            _settings = new global::Application.Settings.FreezingTemperatureSettings { Threshold = 0 };

            _publisherFactoryMock
                .Setup(x => x.GetPublisher("ServiceBus"))
                .Returns(_publisherMock.Object);

            _handler = new FreezingTemperatureAnalysisEventHandler(
                _publisherFactoryMock.Object,
                _loggerMock.Object,
                _settings);
        }

        [Fact]
        public async Task HandleAsync_WhenTemperatureBelowFreezing_ShouldPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, -2, 0, DateTime.UtcNow, "alerts@farm.com"); // -2°C < 0°C threshold
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync("notifications-queue", It.IsAny<object>(), It.IsAny<IDictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTemperatureAboveFreezing_ShouldNotPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, 5, 10, DateTime.UtcNow, "alerts@farm.com"); // 5°C > 0°C threshold
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync(It.IsAny<string>(), It.IsAny<object>(), null),
                Times.Never);
        }

        [Theory]
        [InlineData(-0.1)] // Just below freezing
        [InlineData(-5)]   // Cold
        [InlineData(-10)]  // Very cold
        public async Task HandleAsync_WhenFreezingTemperature_ShouldPublishAlert(decimal temperature)
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, temperature, 0, DateTime.UtcNow, "alerts@farm.com");
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync("notifications-queue", It.IsAny<object>(), It.IsAny<IDictionary<string, object>>()),
                Times.Once);
        }
    }
}
