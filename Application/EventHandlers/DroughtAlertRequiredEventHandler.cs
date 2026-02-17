using Application.Interfaces;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    // #SOLID - Single Responsibility Principle (SRP)
    // Este handler tem uma única responsabilidade: processar o evento DroughtAlertRequiredEvent.
    // Envia alerta para fila do Service Bus quando condições de seca são detectadas.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Depende de IMessagePublisherFactory (abstração), não de ServiceBusPublisher diretamente.
    
    /// <summary>
    /// Handler responsável por processar o evento DroughtAlertRequiredEvent.
    /// Envia alerta para a fila "alert-required-queue" no Azure Service Bus.
    /// </summary>
    public class DroughtAlertRequiredEventHandler : IDomainEventHandler<DroughtAlertRequiredEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<DroughtAlertRequiredEventHandler> _logger;

        public DroughtAlertRequiredEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<DroughtAlertRequiredEventHandler> logger)
        {
            _publisherFactory = publisherFactory ?? throw new ArgumentNullException(nameof(publisherFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(DroughtAlertRequiredEvent domainEvent)
        {
            _logger.LogWarning(
                "Processing DroughtAlertRequiredEvent | FieldId: {FieldId}, CurrentMoisture: {Moisture}%, FirstDetected: {FirstDetected}",
                domainEvent.FieldId,
                domainEvent.CurrentSoilMoisture,
                domainEvent.FirstLowMoistureDetected);

            try
            {
                // Publica alerta no Azure Service Bus
                var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");
                
                var alertMessage = new
                {
                    AlertType = "DroughtCondition",
                    FieldId = domainEvent.FieldId,
                    CurrentSoilMoisture = domainEvent.CurrentSoilMoisture,
                    FirstLowMoistureDetected = domainEvent.FirstLowMoistureDetected,
                    DetectedAt = domainEvent.OccurredOn,
                    Severity = "High",
                    Message = $"Alerta de Seca: Campo {domainEvent.FieldId} com umidade abaixo de 30% por mais de 24 horas. Umidade atual: {domainEvent.CurrentSoilMoisture}%"
                };

                await serviceBusPublisher.PublishMessageAsync("alert-required-queue", alertMessage);

                _logger.LogInformation(
                    "Drought alert sent to queue 'alert-required-queue' for field {FieldId}",
                    domainEvent.FieldId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send drought alert for field {FieldId}",
                    domainEvent.FieldId);
                throw;
            }
        }
    }
}
