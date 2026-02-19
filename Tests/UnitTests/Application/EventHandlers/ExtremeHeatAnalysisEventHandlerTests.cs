using Application.EventHandlers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FieldMeasurement = global::Domain.Entities.FieldMeasurement;
using MeasurementCreatedEvent = global::Domain.Events.MeasurementCreatedEvent;

namespace Tests.UnitTests.Application.EventHandlers
{
    public class ExtremeHeatAnalysisEventHandlerTests
    {
        private readonly Mock<IMessagePublisherFactory> _publisherFactoryMock;
        private readonly Mock<IMessagePublisher> _publisherMock;
        private readonly Mock<ILogger<ExtremeHeatAnalysisEventHandler>> _loggerMock;
        private readonly global::Application.Settings.ExtremeHeatSettings _settings;
        private readonly ExtremeHeatAnalysisEventHandler _handler;

        public ExtremeHeatAnalysisEventHandlerTests()
        {
            _publisherFactoryMock = new Mock<IMessagePublisherFactory>();
            _publisherMock = new Mock<IMessagePublisher>();
            _loggerMock = new Mock<ILogger<ExtremeHeatAnalysisEventHandler>>();
            _settings = new global::Application.Settings.ExtremeHeatSettings { Threshold = 40 };

            _publisherFactoryMock
                .Setup(x => x.GetPublisher("ServiceBus"))
                .Returns(_publisherMock.Object);

            _handler = new ExtremeHeatAnalysisEventHandler(
                _publisherFactoryMock.Object,
                _loggerMock.Object,
                _settings);
        }

        [Fact]
        public async Task HandleAsync_WhenTemperatureExceedsThreshold_ShouldPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, 45, 10, DateTime.UtcNow, "alerts@farm.com"); // 45°C > 40°C threshold
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync("alert-required-queue", It.IsAny<object>(), null),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTemperatureBelowThreshold_ShouldNotPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, 35, 10, DateTime.UtcNow, "alerts@farm.com"); // 35°C < 40°C threshold
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync(It.IsAny<string>(), It.IsAny<object>(), null),
                Times.Never);
        }

        [Theory]
        [InlineData(40.1)] // Just above threshold
        [InlineData(50)]   // Very hot
        [InlineData(60)]   // Extremely hot
        public async Task HandleAsync_WhenHighTemperature_ShouldPublishAlert(decimal temperature)
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, temperature, 0, DateTime.UtcNow, "alerts@farm.com");
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync("alert-required-queue", It.IsAny<object>(), null),
                Times.Once);
        }
    }
}
