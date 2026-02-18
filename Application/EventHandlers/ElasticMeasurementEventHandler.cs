using Application.Interfaces;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Event handler that sends field measurements to Elasticsearch when created.
    /// Uses the measurement object directly from the event to avoid re-querying.
    /// </summary>
    public class ElasticMeasurementEventHandler : IDomainEventHandler<MeasurementCreatedEvent>
    {
        private readonly IElasticService _elasticService;
        private readonly ILogger<ElasticMeasurementEventHandler> _logger;

        public ElasticMeasurementEventHandler(
            IElasticService elasticService,
            ILogger<ElasticMeasurementEventHandler> logger)
        {
            _elasticService = elasticService ?? throw new ArgumentNullException(nameof(elasticService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(MeasurementCreatedEvent domainEvent)
        {
            try
            {
                // Send measurement directly to Elasticsearch (no wrapper needed!)
                var success = await _elasticService.SendToElasticAsync(
                    data: domainEvent.Measurement,
                    indexType: "measurements",
                    documentId: domainEvent.Measurement.Id.ToString());

                if (success)
                {
                    _logger.LogInformation(
                        "Measurement {MeasurementId} sent to Elasticsearch for analytics. FieldId: {FieldId}, SoilMoisture: {SoilMoisture}%",
                        domainEvent.Measurement.Id,
                        domainEvent.Measurement.FieldId,
                        domainEvent.Measurement.SoilMoisture);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to send measurement {MeasurementId} to Elasticsearch. Data is safe in CosmosDB.",
                        domainEvent.Measurement.Id);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - Elasticsearch is secondary storage
                _logger.LogWarning(ex,
                    "Exception while sending measurement {MeasurementId} to Elasticsearch. Data is safe in CosmosDB.",
                    domainEvent.Measurement.Id);
            }
        }
    }
}
