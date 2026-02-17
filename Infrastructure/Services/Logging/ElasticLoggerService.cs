using Application.Interfaces;
using Application.Settings;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Logging
{
    /// <summary>
    /// Serviço de logging para Elasticsearch.
    /// Envia logs estruturados para índices do Elastic.
    /// </summary>
    public class ElasticLoggerService : ILoggerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ElasticLoggerService> _logger;
        private readonly ElasticLoggerSettings _settings;

        public ElasticLoggerService(
            IHttpClientFactory httpClientFactory,
            ILogger<ElasticLoggerService> logger,
            IOptions<ElasticLoggerSettings> settings)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

            _logger.LogInformation("ElasticLoggerService initialized with endpoint: {Endpoint}", _settings.Endpoint);
        }

        public async Task LogRequestAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            try
            {
                await SendToElasticAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send request log to Elastic for LogId: {LogId}", logEntry.LogId);
                throw;
            }
        }

        public async Task UpdateRequestLogAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            try
            {
                await SendToElasticAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send updated request log to Elastic for LogId: {LogId}", logEntry.LogId);
                throw;
            }
        }

        private async Task SendToElasticAsync<T>(T logObject) where T : class
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint))
            {
                _logger.LogWarning("Elastic logs endpoint not configured. Log will not be sent.");
                return;
            }

            var indexPrefix = _settings.IndexPrefix ?? "agro-logs";
            var indexName = logObject switch
            {
                RequestLog => $"{indexPrefix}-requests",
                _ => $"{indexPrefix}-general"
            };

            var indexDate = DateTime.UtcNow.ToString("yyyy.MM.dd");
            var fullIndexName = $"{indexName}-{indexDate}";

            var documentId = logObject switch
            {
                RequestLog log => log.LogId.ToString(),
                _ => Guid.NewGuid().ToString()
            };

            var client = _httpClientFactory.CreateClient();

            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {_settings.ApiKey}");
            }

            var json = JsonSerializer.Serialize(logObject, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = _settings.Endpoint.TrimEnd('/');
            var url = $"{baseUrl}/{fullIndexName}/_doc/{documentId}";

            var response = await client.PutAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send log to Elastic. Status: {Status}, Error: {Error}",
                    response.StatusCode, error);
            }
        }
    }
}
