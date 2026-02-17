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
    // Esta classe tem uma única responsabilidade: gerenciar a lógica de negócio de medições de campo.
    // Ela não se preocupa com detalhes de infraestrutura (DB, mensageria, logs), delegando para outras abstrações.
    
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
            
            // Salvar medição
            var savedMeasurement = await _repository.AddAsync(measurement);
            _logger.LogInformation(
                "Measurement {MeasurementId} saved for field {FieldId}. SoilMoisture: {SoilMoisture}%", 
                savedMeasurement.Id, 
                savedMeasurement.FieldId, 
                savedMeasurement.SoilMoisture);

            // Verificar condições de alerta de seca
            await CheckDroughtConditionsAsync(savedMeasurement);

            return savedMeasurement.ToResponse();
        }

        public async Task<FieldMeasurementResponse> GetMeasurementByIdAsync(Guid id)
        {
            var measurement = await _repository.GetByIdAsync(id);
            
            if (measurement == null)
                throw new KeyNotFoundException($"Measurement with ID {id} not found.");

            return measurement.ToResponse();
        }

        public async Task<IEnumerable<FieldMeasurementResponse>> GetMeasurementsByFieldIdAsync(Guid fieldId)
        {
            var measurements = await _repository.GetByFieldIdAsync(fieldId);
            return measurements.Select(m => m.ToResponse()).ToList();
        }

        public async Task<PagedResponse<FieldMeasurementResponse>> GetMeasurementsPaginatedAsync(
            PaginationParameters paginationParams)
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

        /// <summary>
        /// Verifica se há condições de seca prolongada (umidade < 30% por mais de 24h).
        /// Se detectado, dispara evento de alerta.
        /// </summary>
        private async Task CheckDroughtConditionsAsync(Domain.Entities.FieldMeasurement currentMeasurement)
        {
            const decimal droughtThreshold = 30m;
            const int droughtHours = 24;

            // Se umidade atual está OK, não precisa verificar
            if (currentMeasurement.SoilMoisture >= droughtThreshold)
                return;

            // Buscar medições das últimas 24h
            var endDate = currentMeasurement.CollectedAt;
            var startDate = endDate.AddHours(-droughtHours);

            var recentMeasurements = await _repository.GetByFieldIdAndDateRangeAsync(
                currentMeasurement.FieldId,
                startDate,
                endDate);

            var orderedMeasurements = recentMeasurements
                .OrderBy(m => m.CollectedAt)
                .ToList();

            // Se não há medições suficientes, não dispara alerta
            if (!orderedMeasurements.Any())
                return;

            // Verificar se TODAS as medições nas últimas 24h estão abaixo do threshold
            var allBelowThreshold = orderedMeasurements.All(m => m.SoilMoisture < droughtThreshold);

            if (allBelowThreshold)
            {
                var firstLowMoisture = orderedMeasurements.First().CollectedAt;
                var duration = endDate - firstLowMoisture;

                // Só dispara se passou 24h ou mais
                if (duration.TotalHours >= droughtHours)
                {
                    _logger.LogWarning(
                        "Drought condition detected for field {FieldId}. Moisture below {Threshold}% for {Hours} hours.",
                        currentMeasurement.FieldId,
                        droughtThreshold,
                        duration.TotalHours);

                    var droughtEvent = new DroughtAlertRequiredEvent(
                        currentMeasurement.FieldId,
                        currentMeasurement.SoilMoisture,
                        firstLowMoisture);

                    await _eventDispatcher.ProcessAsync(new[] { droughtEvent });
                }
            }
        }
    }
}
