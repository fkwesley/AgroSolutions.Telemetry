using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Logging
{
    /// <summary>
    /// Serviço de logging que delega para o ILogger configurado (Serilog -> Elastic).
    /// Mantido para compatibilidade, mas não persiste em banco de dados.
    /// 
    /// NOTA: Para persistir logs, use:
    /// - Provider = "Elastic" (recomendado)
    /// - Provider = "NewRelic"
    /// 
    /// Logs são enviados via Serilog para Elasticsearch automaticamente.
    /// </summary>
    public class DatabaseLoggerService : ILoggerService
    {
        private readonly ILogger<DatabaseLoggerService> _logger;

        public DatabaseLoggerService(ILogger<DatabaseLoggerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("DatabaseLoggerService initialized (delegates to Serilog -> Elastic)");
        }

        public Task LogRequestAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            // Delega para Serilog (que envia para Elastic via configuração)
            _logger.LogInformation(
                "HTTP {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | User: {UserId}",
                logEntry.Method,
                logEntry.Path,
                logEntry.StatusCode,
                logEntry.ResponseTimeMs,
                logEntry.UserId ?? "anonymous");

            return Task.CompletedTask;
        }

        public Task UpdateRequestLogAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            if (logEntry.StatusCode == 0)
                return Task.CompletedTask;

            // Delega para Serilog (que envia para Elastic via configuração)
            _logger.LogInformation(
                "HTTP {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | User: {UserId}",
                logEntry.Method,
                logEntry.Path,
                logEntry.StatusCode,
                logEntry.ResponseTimeMs,
                logEntry.UserId ?? "anonymous");

            return Task.CompletedTask;
        }
    }
}
