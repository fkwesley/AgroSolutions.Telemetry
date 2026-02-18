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
    /// Event Handler que executa análise de risco de pragas quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Busca histórico necessário (14 dias)
    /// - Executa análise de risco de pragas via Domain Service
    /// - Publica alerta DIRETO no Service Bus se detectar risco médio ou superior
    /// </summary>
    public class PestRiskAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IFieldMeasurementRepository _repository;
        private readonly IPestRiskAnalysisService _pestRiskService;
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<PestRiskAnalysisEventHandler> _logger;
        private readonly PestRiskSettings _settings;

        public PestRiskAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IPestRiskAnalysisService pestRiskService,
            IMessagePublisherFactory publisherFactory,
            ILogger<PestRiskAnalysisEventHandler> logger,
            PestRiskSettings settings)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _pestRiskService = pestRiskService ?? throw new ArgumentNullException(nameof(pestRiskService));
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
            var pestRisk = _pestRiskService.Analyze(
                history,
                _settings.MinTemperature,
                _settings.MaxTemperature,
                _settings.MinMoisture,
                _settings.MinimumFavorableDays);

            // Se detectou risco médio ou superior, publicar DIRETO no Service Bus
            if (pestRisk != null && pestRisk.RiskLevel >= PestRiskLevel.Medium)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var alertMessage = new
                    {
                        AlertType = "PestRisk",
                        FieldId = measurement.FieldId,
                        RiskLevel = pestRisk.RiskLevel.ToString(),
                        FavorableDaysCount = pestRisk.FavorableDaysCount,
                        AverageTemperature = pestRisk.AverageTemperature,
                        AverageMoisture = pestRisk.AverageMoisture,
                        RiskFactors = pestRisk.RiskFactors,
                        DetectedAt = DateTime.UtcNow,
                        Severity = pestRisk.RiskLevel == PestRiskLevel.High ? "High" : "Medium",
                        Message = $"Risco de Pragas: Campo {measurement.FieldId} - Nível: {pestRisk.RiskLevel}, {pestRisk.FavorableDaysCount} dias consecutivos com condições favoráveis"
                    };

                    await serviceBusPublisher.PublishMessageAsync("alert-required-queue", alertMessage);

                    _logger.LogWarning(
                        "Pest risk alert sent to Service Bus | Field: {FieldId}, Risk: {RiskLevel}, Days: {Days}",
                        measurement.FieldId,
                        pestRisk.RiskLevel,
                        pestRisk.FavorableDaysCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send pest risk alert for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}
