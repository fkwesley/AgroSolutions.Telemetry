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

        public HeatStressAnalysisEventHandler(
            IFieldMeasurementRepository repository,
            IHeatStressAnalysisService heatStressService,
            IMessagePublisherFactory publisherFactory,
            ILogger<HeatStressAnalysisEventHandler> logger,
            HeatStressSettings settings)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _heatStressService = heatStressService ?? throw new ArgumentNullException(nameof(heatStressService));
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

                    var severityLevel = heatStress.Level == Domain.ValueObjects.HeatStressLevel.Severe ? "High" : "Medium";
                    var NotificationRequest = new NotificationRequest
                    {
                        EmailTo = new List<string> { measurement.AlertEmailTo },
                        EmailCc = new List<string>(),
                        EmailBcc = new List<string>(),
                        Subject = string.Format(AlertMessagesConstant.HeatStress.SubjectTemplate, measurement.FieldId, heatStress.Level),
                        Body = AlertMessagesConstant.HeatStress.GetBody(
                            measurement.FieldId,
                            heatStress.Level.ToString(),
                            (decimal)heatStress.DurationInHours,
                            heatStress.AverageTemperature,
                            heatStress.PeakTemperature,
                            _settings.HistoryHours,
                            _settings.CriticalTemperature,
                            _settings.MinimumDurationHours,
                            DateTime.UtcNow),
                        Metadata = new AlertMetadata
                        {
                            AlertType = "HeatStress",
                            FieldId = measurement.FieldId,
                            DetectedAt = DateTime.UtcNow,
                            Severity = severityLevel
                        }
                    };

                    await serviceBusPublisher.PublishMessageAsync("alert-required-queue", NotificationRequest);

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
