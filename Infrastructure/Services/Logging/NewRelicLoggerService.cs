using Application.Interfaces;
using Application.Settings;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services.Logging
{
    /// <summary>
    /// Serviço de logging para NewRelic.
    /// Envia logs para a plataforma de observabilidade NewRelic.
    /// </summary>
    public class NewRelicLoggerService : ILoggerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NewRelicLoggerService> _logger;
        private readonly NewRelicLoggerSettings _settings;

        public NewRelicLoggerService(
            IHttpClientFactory httpClientFactory,
            ILogger<NewRelicLoggerService> logger,
            IOptions<NewRelicLoggerSettings> settings)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

            _logger.LogInformation("NewRelicLoggerService initialized with endpoint: {Endpoint}", _settings.Endpoint);
        }

        public async Task LogRequestAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            if (logEntry.StatusCode == 0)
                return;

            try
            {
                await SendToNewRelicAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send request log to NewRelic for LogId: {LogId}", logEntry.LogId);
                throw;
            }
        }

        public async Task UpdateRequestLogAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            if (logEntry.StatusCode == 0)
                return;

            try
            {
                await SendToNewRelicAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send updated request log to NewRelic for LogId: {LogId}", logEntry.LogId);
                throw;
            }
        }

        private async Task SendToNewRelicAsync(object logObject)
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint))
            {
                _logger.LogWarning("NewRelic endpoint not configured. Log will not be sent.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.LicenseKey))
            {
                _logger.LogWarning("NewRelic license key not configured. Log will not be sent.");
                return;
            }

            var payload = new[]
            {
                new
                {
                    logs = new object[]
                    {
                        logObject switch
                        {
                            RequestLog requestLog => new
                            {
                                message = $"{requestLog.Method} {requestLog.Path} - {requestLog.StatusCode}",
                                attributes = (object)requestLog
                            },
                            _ => new { message = "Unknown log type", attributes = logObject }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-License-Key", _settings.LicenseKey);

            var response = await client.PostAsync(_settings.Endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send log to NewRelic. Status: {Status}, Error: {Error}",
                    response.StatusCode, error);
            }
        }
    }
}
