using Application.Configuration;
using Application.Interfaces;
using Domain.Events;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
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

        public DroughtAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IDroughtDetectionService droughtDetection,
            IMessagePublisherFactory publisherFactory,
            ILogger<DroughtAnalysisEventHandler> logger,
            DroughtAlertSettings settings)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _droughtDetection = droughtDetection ?? throw new ArgumentNullException(nameof(droughtDetection));
            _publisherFactory = publisherFactory ?? throw new ArgumentNullException(nameof(publisherFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task HandleAsync(MeasurementCreatedEvent domainEvent)
        {
            var measurement = domainEvent.Measurement;

            // Buscar histórico necessário
            var history = await _repository.GetByFieldIdAndDateRangeAsync(
                measurement.FieldId,
                measurement.CollectedAt.AddDays(-_settings.HistoryDays),
                measurement.CollectedAt);

            // Executar análise
            var criteria = new DroughtCriteria(_settings.Threshold, _settings.MinimumDurationHours);
            var drought = _droughtDetection.Detect(history, criteria);

            // Se detectou seca, publicar alerta DIRETO no Service Bus
            if (drought != null)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var alertMessage = new
                    {
                        AlertType = "DroughtCondition",
                        FieldId = measurement.FieldId,
                        CurrentSoilMoisture = measurement.SoilMoisture,
                        FirstLowMoistureDetected = drought.StartTime,
                        DurationInHours = drought.DurationInHours,
                        DetectedAt = DateTime.UtcNow,
                        Severity = "High",
                        Message = $"Alerta de Seca: Campo {measurement.FieldId} com umidade abaixo de {_settings.Threshold}% por {drought.DurationInHours:F1} horas. Umidade atual: {measurement.SoilMoisture}%"
                    };

                    await serviceBusPublisher.PublishMessageAsync("alert-required-queue", alertMessage);

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
