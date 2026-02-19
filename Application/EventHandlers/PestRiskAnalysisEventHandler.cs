using Application.Constants;
using Application.DTO.Alerts;
using Application.Interfaces;
using Application.Settings;
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
                DateTime.UtcNow);

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

                    var severityLevel = pestRisk.RiskLevel == PestRiskLevel.High ? "High" : "Medium";
                    var riskFactorsText = string.Join("\n", pestRisk.RiskFactors.Select(rf => $"- {rf}"));
                    var NotificationRequest = new NotificationRequest
                    {
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Subject = string.Format(AlertMessagesConstant.PestRisk.SubjectTemplate, measurement.FieldId, pestRisk.RiskLevel),
                        Body = AlertMessagesConstant.PestRisk.GetBody(
                            measurement.FieldId,
                            pestRisk.RiskLevel.ToString(),
                            pestRisk.FavorableDaysCount,
                            pestRisk.AverageTemperature,
                            pestRisk.AverageMoisture,
                            riskFactorsText,
                            _settings.HistoryDays,
                            _settings.MinTemperature,
                            _settings.MaxTemperature,
                            _settings.MinMoisture,
                            _settings.MinimumFavorableDays,
                            DateTime.UtcNow),
                        Metadata = new AlertMetadata
                        {
                            AlertType = "PestRisk",
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

