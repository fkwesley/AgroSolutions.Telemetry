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
        private readonly ICorrelationContext _correlationContext;

        public IrrigationAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IIrrigationRecommendationService irrigationService,
            IMessagePublisherFactory publisherFactory,
            ILogger<IrrigationAnalysisEventHandler> logger,
            IrrigationSettings settings,
            ICorrelationContext correlationContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _irrigationService = irrigationService ?? throw new ArgumentNullException(nameof(irrigationService));
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
                    var moistureDeficit = _settings.OptimalMoisture - measurement.SoilMoisture;

                    var urgencyAction = recommendation.Urgency switch
                    {
                        Domain.ValueObjects.IrrigationUrgency.Critical => "⚡ Iniciar irrigação IMEDIATAMENTE - situação crítica",
                        Domain.ValueObjects.IrrigationUrgency.High => "🚨 Iniciar irrigação nas próximas 12-24 horas",
                        Domain.ValueObjects.IrrigationUrgency.Medium => "🕐 Planejar irrigação para as próximas 24-48 horas",
                        Domain.ValueObjects.IrrigationUrgency.Low => "📅 Considerar irrigação nos próximos 2-3 dias",
                        _ => "✅ Monitorar condições do solo"
                    };

                    var notificationRequest = new NotificationRequest
                    {
                        TemplateId = "Irrigation",
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "{fieldId}", measurement.FieldId.ToString() },
                            { "{urgency}", recommendation.Urgency.ToString() },
                            { "{urgencyAction}", urgencyAction },
                            { "{currentMoisture}", measurement.SoilMoisture.ToString("F1") },
                            { "{optimalMoisture}", _settings.OptimalMoisture.ToString("F1") },
                            { "{criticalMoisture}", _settings.CriticalMoisture.ToString("F1") },
                            { "{moistureDeficit}", moistureDeficit.ToString("F1") },
                            { "{waterAmountMM}", recommendation.WaterAmountMM.ToString("F1") },
                            { "{estimatedDurationMinutes}", recommendation.EstimatedDuration.TotalMinutes.ToString("F0") },
                            { "{soilWaterCapacity}", _settings.SoilWaterCapacity.ToString() },
                            { "{historyDays}", _settings.HistoryDays.ToString() },
                            { "{detectedAt}", DateTimeHelper.ConvertUtcToTimeZone(DateTime.UtcNow, "E. South America Standard Time").ToString("dd/MM/yyyy HH:mm:ss") + " (Horário de São Paulo)" },
                            { "{correlationId}", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() }
                        },
                        Metadata = new AlertMetadata
                        {
                            CorrelationId = _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString(),
                            AlertType = "IrrigationRecommendation",
                            FieldId = measurement.FieldId,
                            DetectedAt = DateTime.UtcNow,
                            Severity = severityLevel
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

