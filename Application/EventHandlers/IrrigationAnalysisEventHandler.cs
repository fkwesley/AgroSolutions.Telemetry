using Application.Constants;
using Application.DTO.Alerts;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event Handler que executa análise de irrigação quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Busca histórico necessário (7 dias)
    /// - Executa análise de irrigação via Domain Service
    /// - Publica recomendação DIRETO no Service Bus se necessário
    /// </summary>
    public class IrrigationAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IFieldMeasurementRepository _repository;
        private readonly IIrrigationRecommendationService _irrigationService;
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<IrrigationAnalysisEventHandler> _logger;
        private readonly IrrigationSettings _settings;

        public IrrigationAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IIrrigationRecommendationService irrigationService,
            IMessagePublisherFactory publisherFactory,
            ILogger<IrrigationAnalysisEventHandler> logger,
            IrrigationSettings settings)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _irrigationService = irrigationService ?? throw new ArgumentNullException(nameof(irrigationService));
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
                DateTime.UtcNow);

            // Executar análise
            var recommendation = _irrigationService.Analyze(
                history,
                _settings.OptimalMoisture,
                _settings.CriticalMoisture,
                _settings.SoilWaterCapacity);

            // Se houver recomendação, publicar DIRETO no Service Bus
            if (recommendation != null)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var severityLevel = recommendation.Urgency == Domain.ValueObjects.IrrigationUrgency.Critical ? "High" : "Medium";
                    var NotificationRequest = new NotificationRequest
                    {
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Subject = string.Format(AlertMessagesConstant.Irrigation.SubjectTemplate, measurement.FieldId, recommendation.Urgency),
                        Body = AlertMessagesConstant.Irrigation.GetBody(
                            measurement.FieldId,
                            measurement.SoilMoisture,
                            _settings.OptimalMoisture,
                            recommendation.Urgency.ToString(),
                            recommendation.WaterAmountMM,
                            recommendation.EstimatedDuration.TotalMinutes,
                            _settings.HistoryDays,
                            _settings.CriticalMoisture,
                            _settings.SoilWaterCapacity,
                            DateTime.UtcNow),
                        Metadata = new AlertMetadata
                        {
                            AlertType = "IrrigationRecommendation",
                            FieldId = measurement.FieldId,
                            DetectedAt = DateTime.UtcNow,
                            Severity = severityLevel
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
                        "Irrigation recommendation sent to Service Bus | Field: {FieldId}, Urgency: {Urgency}, Water: {Water}mm",
                        measurement.FieldId,
                        recommendation.Urgency,
                        recommendation.WaterAmountMM);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send irrigation recommendation for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}

