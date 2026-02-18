using Application.DTO.Common;
using Application.DTO.FieldMeasurement;
using Application.Interfaces;
using Application.Mappings;
using Domain.Events;
using Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    // #SOLID - Single Responsibility Principle (SRP)
    // Esta classe tem uma única responsabilidade: ORQUESTRAR casos de uso de medições de campo.
    // As análises são executadas por Event Handlers (cada análise escuta MeasurementCreatedEvent).

    // #SOLID - Dependency Inversion Principle (DIP)
    // FieldMeasurementService depende de ABSTRAÇÕES (interfaces) e não de implementações concretas.
    public class FieldMeasurementService : IFieldMeasurementService
    {
        private readonly IFieldMeasurementRepository _repository;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly ILogger<FieldMeasurementService> _logger;

        public FieldMeasurementService(
            IFieldMeasurementRepository repository,
            IDomainEventDispatcher eventDispatcher,
            ILogger<FieldMeasurementService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FieldMeasurementResponse> AddMeasurementAsync(AddFieldMeasurementRequest request)
        {
            var measurement = request.ToEntity();

            // 1. Save to CosmosDB (primary persistence)
            var savedMeasurement = await _repository.AddAsync(measurement);

            _logger.LogInformation(
                "Measurement {MeasurementId} saved to CosmosDB for field {FieldId}. SoilMoisture: {SoilMoisture}%", 
                savedMeasurement.Id, 
                savedMeasurement.FieldId, 
                savedMeasurement.SoilMoisture);

            // 2. Add creation event after successful persistence
            savedMeasurement.AddDomainEvent(new MeasurementCreatedEvent(savedMeasurement));

            // 3. Dispatch all events
            // MeasurementCreatedEvent will trigger:
            //   - ElasticMeasurementEventHandler (Elasticsearch sync)
            //   - DroughtAnalysisEventHandler (Historical drought analysis)
            //   - IrrigationAnalysisEventHandler (Irrigation recommendation)
            //   - HeatStressAnalysisEventHandler (Heat stress detection)
            //   - PestRiskAnalysisEventHandler (Pest risk assessment)
            // Each handler runs independently and generates its own alerts
            await _eventDispatcher.ProcessAsync(savedMeasurement.DomainEvents);

            // 4. Clear domain events after dispatching
            savedMeasurement.ClearDomainEvents();

            return savedMeasurement.ToResponse();
        }

        public async Task<FieldMeasurementResponse> GetMeasurementByIdAsync(Guid id)
        {
            var measurement = await _repository.GetByIdAsync(id);
            
            if (measurement == null)
                throw new KeyNotFoundException($"Measurement with ID {id} not found.");

            return measurement.ToResponse();
        }

        public async Task<IEnumerable<FieldMeasurementResponse>> GetMeasurementsByFieldIdAsync(int fieldId)
        {
            var measurements = await _repository.GetByFieldIdAsync(fieldId);
            return measurements.Select(m => m.ToResponse()).ToList();
        }

        public async Task<PagedResponse<FieldMeasurementResponse>> GetMeasurementsPaginatedAsync(PaginationParameters paginationParams)
        {
            var measurements = await _repository.GetPaginatedAsync(
                paginationParams.Skip,
                paginationParams.Take);

            var totalCount = await _repository.CountAsync();

            var responses = measurements.Select(m => m.ToResponse()).ToList();

            var pagedResponse = new PagedResponse<FieldMeasurementResponse>(
                data: responses,
                totalCount: totalCount,
                currentPage: paginationParams.Page,
                pageSize: paginationParams.PageSize);

            _logger.LogInformation(
                "Retrieved paginated measurements: Page {Page}/{TotalPages}, PageSize: {PageSize}, Total: {TotalCount}",
                paginationParams.Page,
                pagedResponse.TotalPages,
                paginationParams.PageSize,
                totalCount);

            return pagedResponse;
        }
    }
}
