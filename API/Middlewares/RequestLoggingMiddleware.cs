using Application.Interfaces;
using Domain.Entities;
using System.Diagnostics;
using System.Text;
using DomainLogLevel = Domain.Enums.LogLevel;

namespace API.Middlewares
{
    /// <summary>
    /// Middleware para logging automático de todas as requisições HTTP.
    /// Captura informações detalhadas para auditoria e análise de performance.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // Criar log entry
            var logEntry = new RequestLog
            {
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Method = context.Request.Method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                RequestTime = DateTime.UtcNow,
                UserId = context.User?.FindFirst("user_id")?.Value,
                UserEmail = context.User?.FindFirst("user_email")?.Value,
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString()
            };

            // Capturar request body se necessário (apenas para POST/PUT/PATCH)
            if (ShouldLogRequestBody(context.Request.Method))
            {
                logEntry.RequestBody = await ReadRequestBodyAsync(context.Request);
            }

            // Iniciar cronômetro
            var stopwatch = Stopwatch.StartNew();

            // Substituir response stream para capturar response body
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                // Chamar próximo middleware
                await _next(context);

                stopwatch.Stop();

                // Capturar informações da resposta
                logEntry.StatusCode = context.Response.StatusCode;
                logEntry.ResponseTime = DateTime.UtcNow;
                logEntry.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                logEntry.LogLevel = DetermineLogLevel(context.Response.StatusCode);

                // Capturar response body se necessário
                if (ShouldLogResponseBody(context.Response.StatusCode))
                {
                    logEntry.ResponseBody = await ReadResponseBodyAsync(responseBody);
                }

                // Copiar response para stream original
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                logEntry.StatusCode = 500;
                logEntry.ResponseTime = DateTime.UtcNow;
                logEntry.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                logEntry.ErrorMessage = ex.Message;
                logEntry.StackTrace = ex.StackTrace;
                logEntry.LogLevel = Domain.Enums.LogLevel.Error;

                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;

                // Log assíncrono usando ILoggerService (se configurado)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        var loggerService = scope.ServiceProvider.GetService<ILoggerService>();

                        if (loggerService != null)
                        {
                            await loggerService.UpdateRequestLogAsync(logEntry);
                        }

                        // Log estruturado com Serilog
                        _logger.LogInformation(
                            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                            logEntry.Method,
                            logEntry.Path,
                            logEntry.StatusCode,
                            logEntry.ResponseTimeMs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error logging request to database");
                    }
                });
            }
        }

        private static bool ShouldLogRequestBody(string method)
        {
            return method == "POST" || method == "PUT" || method == "PATCH";
        }

        private static bool ShouldLogResponseBody(int statusCode)
        {
            // Logar response body apenas para erros
            return statusCode >= 400;
        }

        private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
        {
            try
            {
                request.EnableBuffering();

                using var reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                // Limitar tamanho do log
                return body.Length > 5000 ? body.Substring(0, 5000) + "... (truncated)" : body;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<string?> ReadResponseBodyAsync(MemoryStream responseStream)
        {
            try
            {
                responseStream.Seek(0, SeekOrigin.Begin);
                var text = await new StreamReader(responseStream).ReadToEndAsync();
                responseStream.Seek(0, SeekOrigin.Begin);

                // Limitar tamanho do log
                return text.Length > 5000 ? text.Substring(0, 5000) + "... (truncated)" : text;
            }
            catch
            {
                return null;
            }
        }

        private static Domain.Enums.LogLevel DetermineLogLevel(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => Domain.Enums.LogLevel.Error,
                >= 400 => Domain.Enums.LogLevel.Warning,
                _ => Domain.Enums.LogLevel.Info
            };
        }
    }
}
