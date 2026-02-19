using Application.Constants;
using Application.DTO.Alerts;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event Handler que detecta temperatura de congelamento quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Verifica se temperatura está abaixo do ponto de congelamento
    /// - Publica alerta DIRETO no Service Bus se detectar risco de geada
    /// </summary>
    public class FreezingTemperatureAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<FreezingTemperatureAnalysisEventHandler> _logger;
        private readonly FreezingTemperatureSettings _settings;

        public FreezingTemperatureAnalysisEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<FreezingTemperatureAnalysisEventHandler> logger,
            FreezingTemperatureSettings settings)
        {
            _publisherFactory = publisherFactory ?? throw new ArgumentNullException(nameof(publisherFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task HandleAsync(MeasurementCreatedEvent domainEvent)
        {
            var measurement = domainEvent.Measurement;

            // Verificar condição crítica: Temperatura de congelamento
            if (measurement.AirTemperature < _settings.Threshold)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var NotificationRequest = new NotificationRequest
                    {
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Subject = string.Format(AlertMessagesConstant.FreezingTemperature.SubjectTemplate, measurement.FieldId),
                        Body = AlertMessagesConstant.FreezingTemperature.GetBody(
                            measurement.FieldId,
                            measurement.AirTemperature,
                            _settings.Threshold,
                            DateTime.UtcNow),
                        Metadata = new AlertMetadata
                        {
                            AlertType = "FreezingTemperature",
                            FieldId = measurement.FieldId,
                            DetectedAt = DateTime.UtcNow,
                            Severity = "High"
                        }
                    };

                    // Prepare custom properties for Service Bus
                    var customProperties = new Dictionary<string, object>
                    {
                        { "CorrelationId", NotificationRequest.Metadata.CorrelationId },
                        { "AlertType", NotificationRequest.Metadata.AlertType },
                        { "FieldId", NotificationRequest.Metadata.FieldId },
                        { "Severity", NotificationRequest.Metadata.Severity }
                    };

                    await serviceBusPublisher.PublishMessageAsync("notifications-queue", NotificationRequest, customProperties);

                    _logger.LogWarning(
                        "Freezing temperature alert sent to Service Bus | Field: {FieldId}, Temperature: {Temperature}°C, Threshold: {Threshold}°C",
                        measurement.FieldId,
                        measurement.AirTemperature,
                        _settings.Threshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send freezing temperature alert for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}

