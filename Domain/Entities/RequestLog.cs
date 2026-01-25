using System.Collections.Generic;

namespace Domain.Entities
{
    public class RequestLog
    {
        public Guid LogId { get; set; } = Guid.NewGuid();
        
        // CorrelationId: ID compartilhado entre múltiplas APIs para rastrear toda a jornada
        // Ex: API Orders chama API Games - ambos terão o mesmo CorrelationId
        public Guid CorrelationId { get; set; }
        
        // ServiceName: Identifica qual API/serviço gerou este log
        // Ex: "orders-api", "games-api", "payments-api"
        public required string ServiceName { get; set; }
        
        public required string UserId { get; set; } // UserId pode ser nulo se não houver autenticação
        public required string HttpMethod { get; set; }
        public required string Path { get; set; }
        public int StatusCode { get; set; }
        public string? RequestBody { get; set; } = null;
        public string? ResponseBody { get; set; } = null;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
