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

            var level = "INFO";
            var message = "No message provided";

            switch (logObject)
            {
                case RequestLog requestLog:
                    level = requestLog.StatusCode >= 500 ? "ERROR"
                          : requestLog.StatusCode >= 400 ? "WARNING"
                          : "INFO";
                    message = $"{requestLog.HttpMethod} {requestLog.Path} - {requestLog.StatusCode}";
                    break;
            }

            var payload = new[]
            {
                new
                {
                    message,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    attributes = new
                    {
                        level,
                        log = logObject
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
            response.EnsureSuccessStatusCode();
        }
    }
}
