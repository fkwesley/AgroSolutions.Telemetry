using Application.DTO.Alerts;
using Application.Helpers;
using Application.Interfaces;
using Application.Settings;
using Domain.Events;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event Handler que executa análise de estresse térmico quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Busca histórico necessário (24 horas)
    /// - Executa análise de estresse térmico via Domain Service
    /// - Publica alerta DIRETO no Service Bus se detectar estresse
    /// </summary>
    public class HeatStressAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IFieldMeasurementRepository _repository;
        private readonly IHeatStressAnalysisService _heatStressService;
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<HeatStressAnalysisEventHandler> _logger;
        private readonly HeatStressSettings _settings;
        private readonly ICorrelationContext _correlationContext;

        public HeatStressAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IHeatStressAnalysisService heatStressService,
            IMessagePublisherFactory publisherFactory,
            ILogger<HeatStressAnalysisEventHandler> logger,
            HeatStressSettings settings,
            ICorrelationContext correlationContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _heatStressService = heatStressService ?? throw new ArgumentNullException(nameof(heatStressService));
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
                measurement.CollectedAt.AddHours(-_settings.HistoryHours),
                DateTime.UtcNow);

            // Executar análise
            var heatStress = _heatStressService.Analyze(
                history,
                _settings.CriticalTemperature,
                _settings.MinimumDurationHours);

            // Se detectou estresse, publicar DIRETO no Service Bus
            if (heatStress != null)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var severityLevel = heatStress.Level == Domain.ValueObjects.HeatStressLevelEnum.Severe ? "High" : "Medium";

                    var notificationRequest = new NotificationRequest
                    {
                        TemplateId = "HeatStress",
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "{fieldId}", measurement.FieldId.ToString() },
                            { "{stressLevel}", heatStress.Level.ToString() },
                            { "{durationHours}", heatStress.DurationInHours.ToString("F1") },
                            { "{averageTemperature}", heatStress.AverageTemperature.ToString("F1") },
                            { "{peakTemperature}", heatStress.PeakTemperature.ToString("F1") },
                            { "{historyHours}", _settings.HistoryHours.ToString() },
                            { "{criticalTemperature}", _settings.CriticalTemperature.ToString("F1") },
                            { "{minimumDurationHours}", _settings.MinimumDurationHours.ToString() },
                            { "{detectedAt}", DateTimeHelper.ConvertUtcToTimeZone(DateTime.UtcNow, "E. South America Standard Time").ToString("dd/MM/yyyy HH:mm:ss") + " (Horário de São Paulo)" },
                            { "{correlationId}", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() }
                        },
                        Priority = heatStress.Level == Domain.ValueObjects.HeatStressLevelEnum.Severe ? PriorityEnum.Critical : PriorityEnum.High
                    };

                    // Prepare custom properties for Service Bus: only CorrelationId and traceparent
                    var customProperties = new Dictionary<string, object>
                    {
                        { "CorrelationId", _correlationContext.CorrelationId?.ToString() ?? Guid.NewGuid().ToString() },
                        { "traceparent", Activity.Current?.Id ?? string.Empty }
                    };

                    await serviceBusPublisher.PublishMessageAsync("notifications-queue", notificationRequest, customProperties);

                    _logger.LogWarning(
                        "Heat stress alert sent to Service Bus | Field: {FieldId}, Level: {Level}, Duration: {Hours:F1}h, Peak: {Peak}°C",
                        measurement.FieldId,
                        heatStress.Level,
                        heatStress.DurationInHours,
                        heatStress.PeakTemperature);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send heat stress alert for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}

