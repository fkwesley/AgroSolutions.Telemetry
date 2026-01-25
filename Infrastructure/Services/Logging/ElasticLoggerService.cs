using Application.Interfaces;
using Application.Settings;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Logging
{
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

            // Define o nome do índice baseado no tipo de log
            // Isso cria índices separados para cada tipo (requests, traces, etc)
            var indexPrefix = _settings.IndexPrefix ?? "app-logs";
            var indexName = logObject switch
            {
                RequestLog => $"{indexPrefix}-requests",
                _ => $"{indexPrefix}-general"
            };

            // Extrai o LogId para usar como document ID no Elasticsearch
            // Isso permite atualizar o mesmo documento (importante para UpdateRequestLogAsync)
            var documentId = logObject switch
            {
                RequestLog log => log.LogId.ToString(),
                _ => null
            };

            var client = _httpClientFactory.CreateClient();
            
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {_settings.ApiKey}");
            }

            var json = JsonSerializer.Serialize(logObject, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Constrói o endpoint completo: base_url/index_name/_doc/document_id
            var baseUrl = _settings.Endpoint.TrimEnd('/');
            
            HttpResponseMessage response;

            if (!string.IsNullOrWhiteSpace(documentId))
            {
                // PUT com ID - Cria ou atualiza o documento com o ID específico
                // Elasticsearch vai substituir o documento inteiro se já existir
                var endpoint = $"{baseUrl}/{indexName}/_doc/{documentId}";
                response = await client.PutAsync(endpoint, content);
            }
            else
            {
                // POST sem ID - Elasticsearch gera um ID automático
                // Usado como fallback se o LogId estiver ausente
                var endpoint = $"{baseUrl}/{indexName}/_doc";
                response = await client.PostAsync(endpoint, content);
            }

            response.EnsureSuccessStatusCode();
        }
    }
}
