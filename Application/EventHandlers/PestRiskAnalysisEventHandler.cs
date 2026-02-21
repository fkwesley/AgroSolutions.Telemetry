using Application.DTO.Alerts;
using Application.Helpers;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        private readonly ICorrelationContext _correlationContext;

        public PestRiskAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IPestRiskAnalysisService pestRiskService,
            IMessagePublisherFactory publisherFactory,
            ILogger<PestRiskAnalysisEventHandler> logger,
            PestRiskSettings settings,
            ICorrelationContext correlationContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _pestRiskService = pestRiskService ?? throw new ArgumentNullException(nameof(pestRiskService));
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
            var pestRisk = _pestRiskService.Analyze(
                history,
                _settings.MinTemperature,
                _settings.MaxTemperature,
                _settings.MinMoisture,
                _settings.MinimumFavorableDays);

            // Se detectou risco médio ou superior, publicar DIRETO no Service Bus
            if (pestRisk != null && pestRisk.RiskLevel >= PestRiskLevelEnum.Medium)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var severityLevel = pestRisk.RiskLevel == PestRiskLevelEnum.High ? "High" : "Medium";
                    var riskFactorsText = string.Join("\n", pestRisk.RiskFactors.Select(rf => $"- {rf}"));

                    var notificationRequest = new NotificationRequest
                    {
                        TemplateId = "PestRisk",
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "{fieldId}", measurement.FieldId.ToString() },
                            { "{riskLevel}", pestRisk.RiskLevel.ToString() },
                            { "{favorableDaysCount}", pestRisk.FavorableDaysCount.ToString() },
                            { "{averageTemperature}", pestRisk.AverageTemperature.ToString("F1") },
                            { "{averageMoisture}", pestRisk.AverageMoisture.ToString("F1") },
                            { "{riskFactors}", riskFactorsText },
                            { "{historyDays}", _settings.HistoryDays.ToString() },
                            { "{minTemperature}", _settings.MinTemperature.ToString("F1") },
                            { "{maxTemperature}", _settings.MaxTemperature.ToString("F1") },
                            { "{minMoisture}", _settings.MinMoisture.ToString("F1") },
                            { "{minimumFavorableDays}", _settings.MinimumFavorableDays.ToString() },
                            { "{detectedAt}", DateTimeHelper.ConvertUtcToTimeZone(DateTime.UtcNow, "E. South America Standard Time").ToString("dd/MM/yyyy HH:mm:ss") + " (Horário de São Paulo)" },
                            { "{correlationId}", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() }
                        },
                        Priority = pestRisk.RiskLevel == PestRiskLevelEnum.High || pestRisk.RiskLevel == PestRiskLevelEnum.Critical ? PriorityEnum.High : PriorityEnum.Normal
                    };

                    // Prepare custom properties for Service Bus: only CorrelationId and traceparent
                    var customProperties = new Dictionary<string, object>
                    {
                        { "CorrelationId", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() },
                        { "traceparent", Activity.Current?.Id ?? string.Empty }
                    };

                    await serviceBusPublisher.PublishMessageAsync("notifications-queue", notificationRequest, customProperties);
                    
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

