using Application.Configuration;
using Application.Interfaces;
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
                measurement.CollectedAt);

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

                    var alertMessage = new
                    {
                        AlertType = "IrrigationRecommendation",
                        FieldId = measurement.FieldId,
                        CurrentMoisture = measurement.SoilMoisture,
                        TargetMoisture = _settings.OptimalMoisture,
                        Urgency = recommendation.Urgency.ToString(),
                        WaterAmountMM = recommendation.WaterAmountMM,
                        EstimatedDurationMinutes = (int)recommendation.EstimatedDuration.TotalMinutes,
                        DetectedAt = DateTime.UtcNow,
                        Severity = recommendation.Urgency == Domain.ValueObjects.IrrigationUrgency.Critical ? "High" : "Medium",
                        Message = $"Recomendação de Irrigação: Campo {measurement.FieldId} - Urgência: {recommendation.Urgency}, Água necessária: {recommendation.WaterAmountMM:F1}mm (~{recommendation.EstimatedDuration.TotalMinutes:F0} minutos)"
                    };

                    await serviceBusPublisher.PublishMessageAsync("alert-required-queue", alertMessage);

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
