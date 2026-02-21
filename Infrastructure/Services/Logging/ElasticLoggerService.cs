using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Logging
{
    /// <summary>
    /// Logger service for Elasticsearch using the generic ElasticService.
    /// Sends request logs to Elasticsearch for auditing and analytics.
    /// </summary>
    public class ElasticLoggerService : ILoggerService
    {
        private readonly IElasticService _elasticService;
        private readonly ILogger<ElasticLoggerService> _logger;

        public ElasticLoggerService(
            IElasticService elasticService,
            ILogger<ElasticLoggerService> logger)
        {
            _elasticService = elasticService ?? throw new ArgumentNullException(nameof(elasticService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("ElasticLoggerService initialized");
        }

        public async Task LogRequestAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            try
            {
                await _elasticService.SendToElasticAsync(
                    data: logEntry,
                    indexType: "requests",
                    documentId: logEntry.LogId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send request log to Elastic for LogId: {LogId}", logEntry.LogId);
                // Don't throw - logging to Elastic is not critical
            }
        }

        public async Task UpdateRequestLogAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            try
            {
                await _elasticService.SendToElasticAsync(
                    data: logEntry,
                    indexType: "requests",
                    documentId: logEntry.LogId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update request log in Elastic for LogId: {LogId}", logEntry.LogId);
                // Don't throw - logging to Elastic is not critical
            }
        }
    }
}
