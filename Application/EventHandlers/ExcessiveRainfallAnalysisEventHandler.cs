using Application.Configuration;
using Application.Interfaces;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event Handler que detecta chuva excessiva quando uma nova medição é criada.
    /// 
    /// RESPONSABILIDADE:
    /// - Escuta MeasurementCreatedEvent
    /// - Verifica se precipitação excede o limite
    /// - Publica alerta DIRETO no Service Bus se detectar chuva excessiva
    /// </summary>
    public class ExcessiveRainfallAnalysisEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<ExcessiveRainfallAnalysisEventHandler> _logger;
        private readonly ExcessiveRainfallSettings _settings;

        public ExcessiveRainfallAnalysisEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<ExcessiveRainfallAnalysisEventHandler> logger,
            ExcessiveRainfallSettings settings)
        {
            _publisherFactory = publisherFactory ?? throw new ArgumentNullException(nameof(publisherFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task HandleAsync(MeasurementCreatedEvent domainEvent)
        {
            var measurement = domainEvent.Measurement;

            // Verificar condição crítica: Chuva excessiva
            if (measurement.Precipitation > _settings.Threshold)
            {
                try
                {
                    var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");

                    var alertMessage = new
                    {
                        AlertType = "ExcessiveRainfall",
                        FieldId = measurement.FieldId,
                        Precipitation = measurement.Precipitation,
                        Threshold = _settings.Threshold,
                        DetectedAt = DateTime.UtcNow,
                        Severity = "High",
                        Message = $"Chuva Excessiva: Campo {measurement.FieldId} com precipitação de {measurement.Precipitation}mm (limite: {_settings.Threshold}mm)"
                    };

                    await serviceBusPublisher.PublishMessageAsync("alert-required-queue", alertMessage);

                    _logger.LogWarning(
                        "Excessive rainfall alert sent to Service Bus | Field: {FieldId}, Precipitation: {Precipitation}mm, Threshold: {Threshold}mm",
                        measurement.FieldId,
                        measurement.Precipitation,
                        _settings.Threshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send excessive rainfall alert for field {FieldId}",
                        measurement.FieldId);
                    throw;
                }
            }
        }
    }
}
