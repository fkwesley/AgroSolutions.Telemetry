using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Entidade para armazenar logs de requisições HTTP.
    /// Usada para auditoria e análise de uso da API.
    /// NOTA: Esta classe tem setters públicos por ser usada como DTO no middleware.
    /// </summary>
    public class RequestLog : BaseEntity
    {
        public Guid LogId { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public DateTime RequestTime { get; set; }
        public DateTime? ResponseTime { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public LogLevel LogLevel { get; set; }
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }

        public RequestLog()
        {
            LogId = Guid.NewGuid();
            RequestTime = DateTime.UtcNow;
        }
    }
}