using Application.DTO.Common;
using Application.DTO.FieldMeasurement;

namespace Application.Interfaces
{
    // #SOLID - Interface Segregation Principle (ISP)
    // Interface focada apenas em operações de negócio relacionadas a medições de campo.
    public interface IFieldMeasurementService
    {
        /// <summary>
        /// Adiciona uma nova medição de campo.
        /// Verifica regras de negócio e dispara alertas se necessário.
        /// </summary>
        Task<FieldMeasurementResponse> AddMeasurementAsync(AddFieldMeasurementRequest request);

        /// <summary>
        /// Retorna uma medição específica por ID.
        /// </summary>
        Task<FieldMeasurementResponse> GetMeasurementByIdAsync(Guid id);

        /// <summary>
        /// Returns measurements for a specific field.
        /// </summary>
        Task<IEnumerable<FieldMeasurementResponse>> GetMeasurementsByFieldIdAsync(int fieldId);

        /// <summary>
        /// Retorna medições paginadas.
        /// </summary>
        Task<PagedResponse<FieldMeasurementResponse>> GetMeasurementsPaginatedAsync(PaginationParameters paginationParams);
    }
}
