using Application.DTO.Alerts;
using Application.Helpers;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event Handler que detecta chuva excessiva quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Verifica se precipitação excede o limite
    /// - Publica alerta DIRETO no Service Bus se detectar chuva excessiva
    /// </summary>
    public class ExcessiveRainfallAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<ExcessiveRainfallAnalysisEventHandler> _logger;
        private readonly ExcessiveRainfallSettings _settings;
        private readonly ICorrelationContext _correlationContext;

        public ExcessiveRainfallAnalysisEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<ExcessiveRainfallAnalysisEventHandler> logger,
            ExcessiveRainfallSettings settings,
            ICorrelationContext correlationContext)
        {
            _publisherFactory = publisherFactory ?? throw new ArgumentNullException(nameof(publisherFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        }

        public async Task HandleAsync(MeasurementCreatedEvent domainEvent)
        {
            var measurement = domainEvent.Measurement;

            // Verificar condição crítica: Chuva excessiva
            if (measurement.Precipitation > _settings.Threshold)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var excess = measurement.Precipitation - _settings.Threshold;
                    var percentAbove = (excess / _settings.Threshold * 100);

                    var notificationRequest = new NotificationRequest
                    {
                        TemplateId = "ExcessiveRainfall",
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "{fieldId}", measurement.FieldId.ToString() },
                            { "{precipitation}", measurement.Precipitation.ToString("F1") },
                            { "{threshold}", _settings.Threshold.ToString("F1") },
                            { "{excess}", excess.ToString("F1") },
                            { "{percentAbove}", percentAbove.ToString("F1") },
                            { "{detectedAt}", DateTimeHelper.ConvertUtcToTimeZone(DateTime.UtcNow, "E. South America Standard Time").ToString("dd/MM/yyyy HH:mm:ss") + " (Horário de São Paulo)" },
                            { "{correlationId}", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() }
                        },
                        Metadata = new AlertMetadata
                        {
                            CorrelationId = _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString(),
                            AlertType = "ExcessiveRainfall",
                            FieldId = measurement.FieldId,
                            DetectedAt = DateTime.UtcNow,
                            Severity = "High"
                        }
                    };

                    // Prepare custom properties for Service Bus
                    var customProperties = new Dictionary<string, object>
                    {
                        { "CorrelationId", notificationRequest.Metadata.CorrelationId },
                        { "AlertType", notificationRequest.Metadata.AlertType },
                        { "FieldId", notificationRequest.Metadata.FieldId },
                        { "Severity", notificationRequest.Metadata.Severity }
                    };

                    await serviceBusPublisher.PublishMessageAsync("notifications-queue", notificationRequest, customProperties);

                    _logger.LogWarning(
                        "Excessive rainfall alert sent to Service Bus | Field: {FieldId}, Precipitation: {Precipitation}mm, Threshold: {Threshold}mm, CorrelationId: {CorrelationId}",
                        measurement.FieldId,
                        measurement.Precipitation,
                        _settings.Threshold,
                        notificationRequest.Metadata.CorrelationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send excessive rainfall alert for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}