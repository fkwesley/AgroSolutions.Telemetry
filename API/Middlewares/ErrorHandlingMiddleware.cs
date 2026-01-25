using API.Models;
using Application.Exceptions;
using Domain.Exceptions;

namespace API.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(
            RequestDelegate next, 
            ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Executa o próximo middleware ou endpoint da pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Captura exceções e trata/loga
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Recupera os IDs gerados pelo RequestLoggingMiddleware
            var logId = context.Items.ContainsKey("LogId") && context.Items["LogId"] is Guid logIdValue
                ? logIdValue
                : Guid.NewGuid();

            var correlationId = context.Items.ContainsKey("CorrelationId") && context.Items["CorrelationId"] is Guid correlationIdValue
                ? correlationIdValue
                : Guid.NewGuid();

            // SERILOG: Log estruturado com exception completa
            // LogId e CorrelationId já estão no LogContext (via middleware)
            _logger.LogError(ex, 
                "Unhandled exception occurred. LogId: {LogId}, CorrelationId: {CorrelationId}", 
                logId, 
                correlationId);

            // Configura response para o cliente
            context.Response.ContentType = "application/json";

            context.Response.StatusCode = ex switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                BusinessException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            var response = new ErrorResponse
            {
                Message = "An error occurred processing your request.",
                Detail = context.Response.StatusCode != StatusCodes.Status500InternalServerError 
                    ? ex.Message 
                    : "Contact support with the LogId and CorrelationId.",
                LogId = context.Response.StatusCode == StatusCodes.Status500InternalServerError 
                    ? logId 
                    : null
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}