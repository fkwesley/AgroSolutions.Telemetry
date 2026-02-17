using Domain.Entities;

namespace Domain.Repositories
{
    // #SOLID - Dependency Inversion Principle (DIP)
    // A camada de domínio define a interface do repositório (abstração).
    // A infraestrutura implementa essa abstração, invertendo a dependência tradicional.
    // Domain não depende de Infrastructure; Infrastructure depende de Domain.
    
    // #SOLID - Interface Segregation Principle (ISP)
    // Interface focada apenas em operações de persistência de FieldMeasurement.
    public interface IFieldMeasurementRepository
    {
        /// <summary>
        /// Retorna todas as medições de um campo específico.
        /// </summary>
        Task<IEnumerable<FieldMeasurement>> GetByFieldIdAsync(int fieldId);

        /// <summary>
        /// Retorna medições de um campo em um intervalo de tempo.
        /// </summary>
        Task<IEnumerable<FieldMeasurement>> GetByFieldIdAndDateRangeAsync(
            int fieldId, 
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// Retorna uma medição específica por ID.
        /// </summary>
        Task<FieldMeasurement?> GetByIdAsync(Guid id);

        /// <summary>
        /// Adiciona uma nova medição.
        /// </summary>
        Task<FieldMeasurement> AddAsync(FieldMeasurement measurement);

        /// <summary>
        /// Retorna medições paginadas.
        /// </summary>
        Task<IEnumerable<FieldMeasurement>> GetPaginatedAsync(int skip, int take);

        /// <summary>
        /// Conta o total de medições.
        /// </summary>
        Task<int> CountAsync();
    }
}
