using Domain.Entities;

namespace Application.Interfaces
{
    // #SOLID - Interface Segregation Principle (ISP)
    // Interface focada apenas em operações de logging.
    // Clientes não são forçados a depender de métodos que não usam.

    // #SOLID - Dependency Inversion Principle (DIP)
    // Abstração que permite múltiplas implementações (Database, Elastic, NewRelic).

    // Interface para o serviço de logging
    // Define métodos para registrar rastreamentos e logs de requisições
    public interface ILoggerService
    {
        Task LogRequestAsync(RequestLog logEntry);
        Task UpdateRequestLogAsync(RequestLog logEntry);
    }
}
