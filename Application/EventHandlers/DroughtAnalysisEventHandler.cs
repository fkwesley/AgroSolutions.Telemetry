using Application.DTO.Alerts;
using Application.Helpers;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event Handler que executa análise de seca quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Busca histórico necessário (7 dias)
    /// - Executa análise de seca via Domain Service
    /// - Publica alerta DIRETO no Service Bus se detectar seca
    /// </summary>
    public class DroughtAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IFieldMeasurementRepository _repository;
        private readonly IDroughtDetectionService _droughtDetection;
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<DroughtAnalysisEventHandler> _logger;
        private readonly DroughtAlertSettings _settings;
        private readonly ICorrelationContext _correlationContext;

        public DroughtAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IDroughtDetectionService droughtDetection,
            IMessagePublisherFactory publisherFactory,
            ILogger<DroughtAnalysisEventHandler> logger,
            DroughtAlertSettings settings,
            ICorrelationContext correlationContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _droughtDetection = droughtDetection ?? throw new ArgumentNullException(nameof(droughtDetection));
            _publisherFactory = publisherFactory ?? throw new ArgumentNullException(nameof(publisherFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        }

        public async Task HandleAsync(MeasurementCreatedEvent domainEvent)
        {
            var measurement = domainEvent.Measurement;

            // Buscar histórico necessário
            var history = await _repository.GetByFieldIdAndDateRangeAsync(
                measurement.FieldId,
                measurement.CollectedAt.AddDays(-_settings.HistoryDays),
                DateTime.UtcNow);

            // Executar análise
            var drought = _droughtDetection.Detect(
                history,
                _settings.Threshold,
                _settings.MinimumDurationHours);

            // Se detectou seca, publicar alerta DIRETO no Service Bus
            if (drought != null)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var moistureDeficit = _settings.Threshold - measurement.SoilMoisture;
                    var durationDays = drought.DurationInHours / 24;

                    var alertMessage = new NotificationRequest
                    {
                        TemplateId = "Drought",
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "{fieldId}", measurement.FieldId.ToString() },
                            { "{soilMoisture}", measurement.SoilMoisture.ToString("F1") },
                            { "{threshold}", _settings.Threshold.ToString("F1") },
                            { "{moistureDeficit}", moistureDeficit.ToString("F1") },
                            { "{durationHours}", drought.DurationInHours.ToString("F1") },
                            { "{durationDays}", durationDays.ToString("F1") },
                            { "{firstLowMoistureDetected}", DateTimeHelper.ConvertUtcToTimeZone(drought.StartTime, "E. South America Standard Time").ToString("dd/MM/yyyy HH:mm:ss") },
                            { "{detectedAt}", DateTimeHelper.ConvertUtcToTimeZone(DateTime.UtcNow, "E. South America Standard Time").ToString("dd/MM/yyyy HH:mm:ss") + " (Horário de São Paulo)" },
                            { "{historyDays}", _settings.HistoryDays.ToString() },
                            { "{minimumDurationHours}", _settings.MinimumDurationHours.ToString() },
                            { "{correlationId}", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() }
                        },
                        Metadata = new AlertMetadata
                        {
                            CorrelationId = _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString(),
                            AlertType = "DroughtCondition",
                            FieldId = measurement.FieldId,
                            DetectedAt = DateTime.UtcNow,
                            Severity = "High"
                        }
                    };

                    // Prepare custom properties for Service Bus
                    var customProperties = new Dictionary<string, object>
                    {
                        { "CorrelationId", alertMessage.Metadata.CorrelationId },
                        { "AlertType", alertMessage.Metadata.AlertType },
                        { "FieldId", alertMessage.Metadata.FieldId },
                        { "Severity", alertMessage.Metadata.Severity }
                    };

                    await serviceBusPublisher.PublishMessageAsync("notifications-queue", alertMessage, customProperties);

                    _logger.LogWarning(
                        "Drought alert sent to Service Bus | Field: {FieldId}, Moisture: {Moisture}%, Duration: {Hours:F1}h, Threshold: {Threshold}%",
                        measurement.FieldId,
                        measurement.SoilMoisture,
                        drought.DurationInHours,
                        _settings.Threshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send drought alert for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}
