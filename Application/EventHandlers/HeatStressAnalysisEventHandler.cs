using Application.Configuration;
using Application.Interfaces;
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
                measurement.CollectedAt);

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

                    var alertMessage = new
                    {
                        AlertType = "HeatStress",
                        FieldId = measurement.FieldId,
                        StressLevel = heatStress.Level.ToString(),
                        DurationInHours = heatStress.DurationInHours,
                        AverageTemperature = heatStress.AverageTemperature,
                        PeakTemperature = heatStress.PeakTemperature,
                        DetectedAt = DateTime.UtcNow,
                        Severity = heatStress.Level == Domain.ValueObjects.HeatStressLevel.Severe ? "High" : "Medium",
                        Message = $"Estresse Térmico: Campo {measurement.FieldId} - Nível: {heatStress.Level}, Duração: {heatStress.DurationInHours:F1}h, Pico: {heatStress.PeakTemperature:F1}°C"
                    };

                    await serviceBusPublisher.PublishMessageAsync("alert-required-queue", alertMessage);

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
