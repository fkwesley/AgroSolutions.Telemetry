using Application.EventHandlers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FieldMeasurement = global::Domain.Entities.FieldMeasurement;
using DroughtCondition = global::Domain.ValueObjects.DroughtCondition;
using MeasurementCreatedEvent = global::Domain.Events.MeasurementCreatedEvent;

namespace Tests.UnitTests.Application.EventHandlers
{
    public class DroughtAnalysisEventHandlerTests
    {
        private readonly Mock<global::Domain.Repositories.IFieldMeasurementRepository> _repositoryMock;
        private readonly Mock<global::Domain.Services.IDroughtDetectionService> _droughtDetectionMock;
        private readonly Mock<IMessagePublisherFactory> _publisherFactoryMock;
        private readonly Mock<IMessagePublisher> _publisherMock;
        private readonly Mock<ILogger<DroughtAnalysisEventHandler>> _loggerMock;
        private readonly Mock<ICorrelationContext> _correlationContextMock;
        private readonly global::Application.Settings.DroughtAlertSettings _settings;
        private readonly DroughtAnalysisEventHandler _handler;

        public DroughtAnalysisEventHandlerTests()
        {
            _repositoryMock = new Mock<global::Domain.Repositories.IFieldMeasurementRepository>();
            _droughtDetectionMock = new Mock<global::Domain.Services.IDroughtDetectionService>();
            _publisherFactoryMock = new Mock<IMessagePublisherFactory>();
            _publisherMock = new Mock<IMessagePublisher>();
            _loggerMock = new Mock<ILogger<DroughtAnalysisEventHandler>>();
            _correlationContextMock = new Mock<ICorrelationContext>();
            _settings = new global::Application.Settings.DroughtAlertSettings();

            _publisherFactoryMock
                .Setup(x => x.GetPublisher("ServiceBus"))
                .Returns(_publisherMock.Object);

            // Setup CorrelationContext mock
            _correlationContextMock.Setup(x => x.CorrelationId).Returns(Guid.NewGuid());
            _correlationContextMock.Setup(x => x.LogId).Returns(Guid.NewGuid());

            _handler = new DroughtAnalysisEventHandler(
                _repositoryMock.Object,
                _droughtDetectionMock.Object,
                _publisherFactoryMock.Object,
                _loggerMock.Object,
                _settings,
                _correlationContextMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenDroughtDetected_ShouldPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 25, 30, 5, DateTime.UtcNow, "alerts@farm.com");
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            var history = new List<FieldMeasurement>
            {
                new(1, 28, 30, 5, DateTime.UtcNow.AddDays(-1), "alerts@farm.com"),
                new(1, 25, 30, 5, DateTime.UtcNow.AddDays(-2), "alerts@farm.com")
            };

            _repositoryMock
                .Setup(x => x.GetByFieldIdAndDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(history);

            _droughtDetectionMock
                .Setup(x => x.Detect(It.IsAny<IEnumerable<FieldMeasurement>>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .Returns(new DroughtCondition(DateTime.UtcNow.AddDays(-2), TimeSpan.FromHours(48)));

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync("notifications-queue", It.IsAny<object>(), It.IsAny<IDictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenNoDroughtDetected_ShouldNotPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 55, 25, 10, DateTime.UtcNow, "alerts@farm.com");
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            _repositoryMock
                .Setup(x => x.GetByFieldIdAndDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(new List<FieldMeasurement>());

            _droughtDetectionMock
                .Setup(x => x.Detect(It.IsAny<IEnumerable<FieldMeasurement>>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .Returns((DroughtCondition?)null);

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync(It.IsAny<string>(), It.IsAny<object>(), null),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenPublishFails_ShouldThrowException()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 25, 30, 5, DateTime.UtcNow, "alerts@farm.com");
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            _repositoryMock
                .Setup(x => x.GetByFieldIdAndDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(new List<FieldMeasurement>());

            _droughtDetectionMock
                .Setup(x => x.Detect(It.IsAny<IEnumerable<FieldMeasurement>>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .Returns(new DroughtCondition(DateTime.UtcNow.AddDays(-2), TimeSpan.FromHours(48)));

            _publisherMock
                .Setup(x => x.PublishMessageAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDictionary<string, object>>()))
                .ThrowsAsync(new Exception("Service Bus error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.HandleAsync(measurementEvent));
        }
    }
}
