using Application.DTO.Alerts;
using Application.Helpers;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        private readonly ICorrelationContext _correlationContext;

        public FreezingTemperatureAnalysisEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<FreezingTemperatureAnalysisEventHandler> logger,
            FreezingTemperatureSettings settings,
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

            // Verificar condição crítica: Temperatura de congelamento
            if (measurement.AirTemperature < _settings.Threshold)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var temperatureBelowThreshold = _settings.Threshold - measurement.AirTemperature;

                    var notificationRequest = new NotificationRequest
                    {
                        TemplateId = "FreezingTemperature",
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "{fieldId}", measurement.FieldId.ToString() },
                            { "{airTemperature}", measurement.AirTemperature.ToString("F1") },
                            { "{threshold}", _settings.Threshold.ToString("F1") },
                            { "{temperatureBelowThreshold}", temperatureBelowThreshold.ToString("F1") },
                            { "{detectedAt}", DateTimeHelper.ConvertUtcToTimeZone(DateTime.UtcNow, "E. South America Standard Time").ToString("dd/MM/yyyy HH:mm:ss") + " (Horário de São Paulo)" },
                            { "{correlationId}", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() }
                        },
                        Priority = PriorityEnum.Critical
                    };

                    // Prepare custom properties for Service Bus: only CorrelationId and traceparent
                    var customProperties = new Dictionary<string, object>
                    {
                        { "CorrelationId", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() },
                        { "traceparent", Activity.Current?.Id ?? string.Empty }
                    };

                    await serviceBusPublisher.PublishMessageAsync("notifications-queue", notificationRequest, customProperties);

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

