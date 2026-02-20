using Application.EventHandlers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FieldMeasurement = global::Domain.Entities.FieldMeasurement;
using MeasurementCreatedEvent = global::Domain.Events.MeasurementCreatedEvent;

namespace Tests.UnitTests.Application.EventHandlers
{
    public class ExcessiveRainfallAnalysisEventHandlerTests
    {
        private readonly Mock<IMessagePublisherFactory> _publisherFactoryMock;
        private readonly Mock<IMessagePublisher> _publisherMock;
        private readonly Mock<ILogger<ExcessiveRainfallAnalysisEventHandler>> _loggerMock;
        private readonly Mock<ICorrelationContext> _correlationContextMock;
        private readonly global::Application.Settings.ExcessiveRainfallSettings _settings;
        private readonly ExcessiveRainfallAnalysisEventHandler _handler;

        public ExcessiveRainfallAnalysisEventHandlerTests()
        {
            _publisherFactoryMock = new Mock<IMessagePublisherFactory>();
            _publisherMock = new Mock<IMessagePublisher>();
            _loggerMock = new Mock<ILogger<ExcessiveRainfallAnalysisEventHandler>>();
            _correlationContextMock = new Mock<ICorrelationContext>();
            _settings = new global::Application.Settings.ExcessiveRainfallSettings { Threshold = 60 };

            _publisherFactoryMock
                .Setup(x => x.GetPublisher("ServiceBus"))
                .Returns(_publisherMock.Object);

            // Setup CorrelationContext mock
            _correlationContextMock.Setup(x => x.CorrelationId).Returns(Guid.NewGuid());
            _correlationContextMock.Setup(x => x.LogId).Returns(Guid.NewGuid());

            _handler = new ExcessiveRainfallAnalysisEventHandler(
                _publisherFactoryMock.Object,
                _loggerMock.Object,
                _settings,
                _correlationContextMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenPrecipitationExceedsThreshold_ShouldPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, 25, 75, DateTime.UtcNow, "alerts@farm.com"); // 75mm > 60mm threshold
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert - Verify that message was published, without checking content
            _publisherMock.Verify(
                x => x.PublishMessageAsync("notifications-queue", It.IsAny<object>(), It.IsAny<IDictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenPrecipitationBelowThreshold_ShouldNotPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, 25, 40, DateTime.UtcNow, "alerts@farm.com"); // 40mm < 60mm threshold
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync(It.IsAny<string>(), It.IsAny<object>(), null),
                Times.Never);
        }

        [Theory]
        [InlineData(60.1)] // Just above threshold
        [InlineData(100)]  // High rainfall
        [InlineData(200)]  // Extreme rainfall
        public async Task HandleAsync_WhenPrecipitationAboveThreshold_ShouldPublishAlert(decimal precipitation)
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 50, 25, precipitation, DateTime.UtcNow, "alerts@farm.com");
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
