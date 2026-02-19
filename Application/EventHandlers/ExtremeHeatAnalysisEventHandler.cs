using Application.Constants;
using Application.DTO.Alerts;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event Handler que detecta calor extremo quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Verifica se temperatura excede o limite crítico
    /// - Publica alerta DIRETO no Service Bus se detectar calor extremo
    /// </summary>
    public class ExtremeHeatAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<ExtremeHeatAnalysisEventHandler> _logger;
        private readonly ExtremeHeatSettings _settings;

        public ExtremeHeatAnalysisEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<ExtremeHeatAnalysisEventHandler> logger,
            ExtremeHeatSettings settings)
        {
            _publisherFactory = publisherFactory ?? throw new ArgumentNullException(nameof(publisherFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task HandleAsync(MeasurementCreatedEvent domainEvent)
        {
            var measurement = domainEvent.Measurement;

            // Verificar condição crítica: Calor extremo
            if (measurement.AirTemperature > _settings.Threshold)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var NotificationRequest = new NotificationRequest
                    {
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Subject = string.Format(AlertMessagesConstant.ExtremeHeat.SubjectTemplate, measurement.FieldId),
                        Body = AlertMessagesConstant.ExtremeHeat.GetBody(
                            measurement.FieldId,
                            measurement.AirTemperature,
                            _settings.Threshold,
                            DateTime.UtcNow),
                        Metadata = new AlertMetadata
                        {
                            AlertType = "ExtremeHeat",
                            FieldId = measurement.FieldId,
                            DetectedAt = DateTime.UtcNow,
                            Severity = "High"
                        }
                    };

                    await serviceBusPublisher.PublishMessageAsync("alert-required-queue", NotificationRequest);

                    _logger.LogWarning(
                        "Extreme heat alert sent to Service Bus | Field: {FieldId}, Temperature: {Temperature}°C, Threshold: {Threshold}°C",
                        measurement.FieldId,
                        measurement.AirTemperature,
                        _settings.Threshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send extreme heat alert for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}
