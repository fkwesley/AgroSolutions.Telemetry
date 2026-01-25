using API.Models;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private const int MaxBodySize = 1024 * 1024; // 1MB - Limite para evitar consumo excessivo de memória

        public RequestLoggingMiddleware(
            RequestDelegate next,
            IServiceScopeFactory scopeFactory,
            ILogger<RequestLoggingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Ignorar requisições para o Swagger (UI e JSON)
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            // Inicia o timer para calcular a duração da requisição
            var stopwatch = Stopwatch.StartNew();

            // Cria um GUID único para rastreabilidade da requisição (local desta API)
            var logId = Guid.NewGuid();

            // CorrelationId: ID compartilhado entre múltiplas APIs
            // - Se receber no header X-Correlation-Id, usa o mesmo (propagação)
            // - Se não receber, gera novo (primeira API da jornada)
            var correlationId = GetOrCreateCorrelationId(context);

            // Obtém o nome do serviço da configuração
            var serviceName = _configuration.GetValue<string>("LoggerSettings:ServiceName") ?? "unknown-service";

            // Compartilha os IDs com outros middlewares via HttpContext.Items
            // Isso permite que o ErrorHandlingMiddleware use os mesmos IDs para vincular logs de erro
            context.Items["LogId"] = logId;
            context.Items["CorrelationId"] = correlationId;

            // Popula contexto de correlação (AsyncLocal)
            // Isso permite que TODOS os logs técnicos (ILogger<T>) tenham LogId e CorrelationId
            // mesmo em chamadas assíncronas, Service Bus, repositories, etc.
            CorrelationContext.LogId = logId;
            CorrelationContext.CorrelationId = correlationId;
            CorrelationContext.ServiceName = serviceName;
            CorrelationContext.UserId = context.User?.FindFirst("user_id")?.Value;

            // Adiciona os IDs ao Serilog LogContext
            // Todos os logs do Serilog dentro deste scope terão estes valores automaticamente
            // Usa "LogId" consistente com a entidade RequestLog
            using (Serilog.Context.LogContext.PushProperty("LogId", logId))
            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            using (Serilog.Context.LogContext.PushProperty("UserId", context.User?.FindFirst("user_id")?.Value))
            {

                // Adiciona os IDs no header da resposta para o cliente poder rastrear
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.TryAdd("X-Log-Id", logId.ToString());
                    context.Response.Headers.TryAdd("X-Correlation-Id", correlationId.ToString());
                    return Task.CompletedTask;
                });

                // Preparar para leitura do body (permite ler múltiplas vezes)
                context.Request.EnableBuffering();

                // Lê o request body com proteção de tamanho
                var requestBody = await ReadBodySafeAsync(context.Request.Body);
                context.Request.Body.Position = 0; // Reseta para outros middlewares poderem ler

                // Cria log inicial com dados da entrada e IDs de rastreamento
                var logEntry = new RequestLog
                {
                    LogId = logId,
                    CorrelationId = correlationId, // Para rastrear jornada completa entre APIs
                    ServiceName = serviceName, // Identifica qual API gerou este log
                    UserId = context.User?.FindFirst("user_id")?.Value ?? "anonymous",
                    HttpMethod = context.Request.Method,
                    Path = context.Request.Path,
                    RequestBody = requestBody,
                    StartDate = DateTime.UtcNow
                };

                using var scope = _scopeFactory.CreateScope();
                var loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService>();

                // Salva o log inicial para já ter LogId disponível
                // Se falhar, não impede a requisição de prosseguir
                try
                {
                    _ = loggerService.LogRequestAsync(logEntry);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to create initial request log for LogId: {LogId}, CorrelationId: {CorrelationId}",
                        logId, correlationId);
                }

                // Prepara para interceptar o response body
                var originalBodyStream = context.Response.Body;
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                try
                {
                    // Executa o próximo middleware/endpoint da pipeline
                    // Erros aqui serão capturados pelo ErrorHandlingMiddleware
                    await _next(context);
                }
                finally
                {
                    // SEMPRE executa, mesmo se houver erro
                    // Garante que o log seja atualizado com os dados finais
                    stopwatch.Stop();

                    // Lê o response body com proteção de tamanho
                    memoryStream.Position = 0;
                    var responseBody = await ReadBodySafeAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Restaura o response body original
                    await memoryStream.CopyToAsync(originalBodyStream);
                    context.Response.Body = originalBodyStream;

                    // Atualiza log com dados finais (StatusCode, responseBody, duração)
                    logEntry.StatusCode = context.Response.StatusCode;
                    logEntry.ResponseBody = responseBody;
                    logEntry.EndDate = DateTime.UtcNow;
                    logEntry.Duration = stopwatch.Elapsed;

                    // Tenta atualizar o log no sistema de logging
                    // Se falhar, apenas loga o erro mas não impede a resposta ao cliente
                    try
                    {
                        _ = loggerService.UpdateRequestLogAsync(logEntry);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to update request log for LogId: {LogId}, CorrelationId: {CorrelationId}",
                            logId, correlationId);
                    }
                }
            }
        }

        /// <summary>
        /// Obtém o CorrelationId do header X-Correlation-Id ou gera um novo
        /// Isso permite rastrear requisições através de múltiplas APIs
        /// </summary>
        private Guid GetOrCreateCorrelationId(HttpContext context)
        {
            // Tenta obter do header (enviado por outra API upstream)
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationIdHeader))
            {
                if (Guid.TryParse(correlationIdHeader, out var correlationId))
                {
                    // CorrelationId recebido - esta API faz parte de uma jornada maior
                    return correlationId;
                }
            }

            // Não recebeu CorrelationId - esta é a primeira API da jornada
            // Gera novo CorrelationId que será propagado para APIs downstream
            return Guid.NewGuid();
        }

        /// <summary>
        /// Lê o corpo de uma stream com proteção contra payloads muito grandes
        /// </summary>
        private async Task<string?> ReadBodySafeAsync(Stream body)
        {
            if (body == null || !body.CanRead)
                return null;

            try
            {
                using var reader = new StreamReader(body, leaveOpen: true);
                var content = await reader.ReadToEndAsync();

                // Se não tem conteúdo, retorna null
                if (string.IsNullOrEmpty(content))
                    return null;

                // Proteção: se o conteúdo for maior que o limite, retorna mensagem truncada
                if (content.Length > MaxBodySize)
                {
                    _logger.LogWarning("Request body too large: {Size} bytes (max: {MaxSize})",
                        content.Length, MaxBodySize);
                    return $"[Body too large: {content.Length} bytes, max allowed: {MaxBodySize} bytes]";
                }

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read request/response body");
                return "[Failed to read body]";
            }
        }
    }
}

