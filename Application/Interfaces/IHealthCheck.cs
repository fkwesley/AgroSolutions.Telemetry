using Application.DTO.Health;

namespace Application.Interfaces
{
    /// <summary>
    /// Base interface for all health checks
    /// #SOLID - Open/Closed Principle (OCP)
    /// Novos health checks podem ser adicionados sem modificar código existente
    /// </summary>
    public interface IHealthCheck
    {
        /// <summary>
        /// Nome do componente sendo verificado (ex: "Database", "RabbitMQ", "Elasticsearch")
        /// </summary>
        string ComponentName { get; }

        /// <summary>
        /// Indica se o componente é crítico (se falhar, API retorna 503)
        /// </summary>
        bool IsCritical { get; }

        /// <summary>
        /// Executa verificação de saúde do componente
        /// </summary>
        /// <returns>Status de saúde com detalhes</returns>
        Task<ComponentHealth> CheckHealthAsync();
    }
}
