using Application.DTO.Health;
using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace Infrastructure.Services.HealthCheck
{
    /// <summary>
    /// Elasticsearch Health Check Implementation
    /// #SOLID - Single Responsibility Principle (SRP)
    /// Verifica a disponibilidade do cluster Elasticsearch com autenticação
    /// </summary>
    public class ElasticsearchHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ElasticLoggerSettings _elasticSettings;
        private readonly ILogger<ElasticsearchHealthCheck> _logger;

        public string ComponentName => "Elasticsearch";
        public bool IsCritical => false; // Não-crítico - API funciona sem Elasticsearch (logs podem atrasar)

        public ElasticsearchHealthCheck(
            IHttpClientFactory httpClientFactory,
            IOptions<ElasticLoggerSettings> elasticSettings,
            ILogger<ElasticsearchHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _elasticSettings = elasticSettings?.Value ?? throw new ArgumentNullException(nameof(elasticSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentHealth> CheckHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var elasticUrl = _elasticSettings.Endpoint;

                if (string.IsNullOrEmpty(elasticUrl))
                {
                    return new ComponentHealth
                    {
                        Status = "Unknown",
                        Description = "Elasticsearch URL not configured"
                    };
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                // #FIX - Adiciona autenticação (suporta API Key ou Basic Auth)
                if (!string.IsNullOrEmpty(_elasticSettings.ApiKey))
                {
                    // API Key authentication (Elasticsearch 7.x+)
                    // Formato: Authorization: ApiKey <base64_encoded_api_key>
                    client.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("ApiKey", _elasticSettings.ApiKey);
                    
                    _logger.LogDebug("Elasticsearch health check using API Key authentication");
                }
                else
                {
                    _logger.LogWarning("Elasticsearch health check without authentication (credentials not configured)");
                }


                // Verifica a saúde do cluster Elasticsearch
                var response = await client.GetAsync($"{elasticUrl}/_cluster/health");
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    return new ComponentHealth
                    {
                        Status = "Healthy",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Description = "Elasticsearch cluster is healthy"
                    };
                }

                // Log detalhado para troubleshooting
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning(
                        "Elasticsearch health check returned 401 Unauthorized. Check API Key configuration.");
                }

                return new ComponentHealth
                {
                    Status = "Degraded",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = $"Elasticsearch returned {response.StatusCode}"
                };
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Elasticsearch health check timeout");

                return new ComponentHealth
                {
                    Status = "Degraded",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = "Elasticsearch timeout (logs may be delayed)"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Elasticsearch health check failed");

                return new ComponentHealth
                {
                    Status = "Degraded",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = $"Elasticsearch not available: {ex.Message}"
                };
            }
        }
    }
}

