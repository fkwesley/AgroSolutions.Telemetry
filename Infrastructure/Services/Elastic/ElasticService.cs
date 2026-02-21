using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Elastic
{
    /// <summary>
    /// Generic service for sending any data to Elasticsearch.
    /// Handles index creation, authentication, and error handling.
    /// </summary>
    public class ElasticService : IElasticService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ElasticService> _logger;
        private readonly ElasticLoggerSettings _settings;

        public ElasticService(
            IHttpClientFactory httpClientFactory,
            ILogger<ElasticService> logger,
            IOptions<ElasticLoggerSettings> settings)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Sends any object to Elasticsearch with specified index type.
        /// </summary>
        /// <typeparam name="T">Type of object to send</typeparam>
        /// <param name="data">Data to send to Elasticsearch</param>
        /// <param name="indexType">Index type (e.g., "requests", "measurements", "alerts")</param>
        /// <param name="documentId">Optional document ID. If null, generates a new GUID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SendToElasticAsync<T>(T data, string indexType, string? documentId = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(data);
            
            if (string.IsNullOrWhiteSpace(indexType))
                throw new ArgumentException("Index type cannot be null or empty", nameof(indexType));

            if (string.IsNullOrWhiteSpace(_settings.Endpoint))
            {
                _logger.LogWarning("Elasticsearch endpoint not configured. Data will not be sent.");
                return false;
            }

            try
            {
                // Build index name: agro-{indexType}
                var indexPrefix = _settings.IndexPrefix ?? "agro";
                var indexName = $"{indexPrefix}-{indexType}";

                // Generate document ID if not provided
                var docId = documentId ?? Guid.NewGuid().ToString();

                // Create HTTP client
                var client = _httpClientFactory.CreateClient();

                // Add authentication if API key is configured
                if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {_settings.ApiKey}");
                }

                // Serialize object to JSON
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Build Elasticsearch URL
                var baseUrl = _settings.Endpoint.TrimEnd('/');
                var url = $"{baseUrl}/{indexName}/_doc/{docId}";

                // Send to Elasticsearch
                var response = await client.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Failed to send data to Elasticsearch. Index: {Index}, Status: {Status}, Error: {Error}",
                        indexName, response.StatusCode, error);
                    return false;
                }

                _logger.LogDebug(
                    "Successfully sent data to Elasticsearch. Index: {Index}, DocumentId: {DocumentId}",
                    indexName, docId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Exception while sending data to Elasticsearch. IndexType: {IndexType}",
                    indexType);
                return false;
            }
        }

        /// <summary>
        /// Sends data to Elasticsearch asynchronously in background (fire-and-forget).
        /// Logs errors but doesn't throw exceptions.
        /// </summary>
        public void SendToElasticInBackground<T>(T data, string indexType, string? documentId = null) where T : class
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendToElasticAsync(data, indexType, documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Background task failed to send data to Elasticsearch. IndexType: {IndexType}",
                        indexType);
                }
            });
        }
    }
}
