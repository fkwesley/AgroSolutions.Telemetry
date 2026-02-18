using Application.EventHandlers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FieldMeasurement = global::Domain.Entities.FieldMeasurement;
using DroughtCriteria = global::Domain.ValueObjects.DroughtCriteria;
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
        private readonly global::Application.Configuration.DroughtAlertSettings _settings;
        private readonly DroughtAnalysisEventHandler _handler;

        public DroughtAnalysisEventHandlerTests()
        {
            _repositoryMock = new Mock<global::Domain.Repositories.IFieldMeasurementRepository>();
            _droughtDetectionMock = new Mock<global::Domain.Services.IDroughtDetectionService>();
            _publisherFactoryMock = new Mock<IMessagePublisherFactory>();
            _publisherMock = new Mock<IMessagePublisher>();
            _loggerMock = new Mock<ILogger<DroughtAnalysisEventHandler>>();
            _settings = new global::Application.Configuration.DroughtAlertSettings();

            _publisherFactoryMock
                .Setup(x => x.GetPublisher("ServiceBus"))
                .Returns(_publisherMock.Object);

            _handler = new DroughtAnalysisEventHandler(
                _repositoryMock.Object,
                _droughtDetectionMock.Object,
                _publisherFactoryMock.Object,
                _loggerMock.Object,
                _settings);
        }

        [Fact]
        public async Task HandleAsync_WhenDroughtDetected_ShouldPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 25, 30, 5, DateTime.UtcNow);
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            var history = new List<FieldMeasurement>
            {
                new(1, 28, 30, 5, DateTime.UtcNow.AddDays(-1)),
                new(1, 25, 30, 5, DateTime.UtcNow.AddDays(-2))
            };

            _repositoryMock
                .Setup(x => x.GetByFieldIdAndDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(history);

            _droughtDetectionMock
                .Setup(x => x.Detect(It.IsAny<IEnumerable<FieldMeasurement>>(), It.IsAny<DroughtCriteria>()))
                .Returns(new DroughtCondition(DateTime.UtcNow.AddDays(-2), TimeSpan.FromHours(48)));

            // Act
            await _handler.HandleAsync(measurementEvent);

            // Assert
            _publisherMock.Verify(
                x => x.PublishMessageAsync("alert-required-queue", It.IsAny<object>(), null),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenNoDroughtDetected_ShouldNotPublishAlert()
        {
            // Arrange
            var measurement = new FieldMeasurement(1, 55, 25, 10, DateTime.UtcNow);
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            _repositoryMock
                .Setup(x => x.GetByFieldIdAndDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(new List<FieldMeasurement>());

            _droughtDetectionMock
                .Setup(x => x.Detect(It.IsAny<IEnumerable<FieldMeasurement>>(), It.IsAny<DroughtCriteria>()))
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
            var measurement = new FieldMeasurement(1, 25, 30, 5, DateTime.UtcNow);
            var measurementEvent = new MeasurementCreatedEvent(measurement);

            _repositoryMock
                .Setup(x => x.GetByFieldIdAndDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(new List<FieldMeasurement>());

            _droughtDetectionMock
                .Setup(x => x.Detect(It.IsAny<IEnumerable<FieldMeasurement>>(), It.IsAny<DroughtCriteria>()))
                .Returns(new DroughtCondition(DateTime.UtcNow.AddDays(-2), TimeSpan.FromHours(48)));

            _publisherMock
                .Setup(x => x.PublishMessageAsync(It.IsAny<string>(), It.IsAny<object>(), null))
                .ThrowsAsync(new Exception("Service Bus error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.HandleAsync(measurementEvent));
        }
    }
}
