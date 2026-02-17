using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    // #SOLID - Single Responsibility Principle (SRP)
    // FieldMeasurementRepository tem uma única responsabilidade: gerenciar a persistência de medições no CosmosDB.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Implementa a interface IFieldMeasurementRepository definida no domínio.
    
    /// <summary>
    /// Repositório para persistência de FieldMeasurement no Azure CosmosDB.
    /// Utiliza o SDK do CosmosDB para operações CRUD.
    /// 
    /// BOAS PRÁTICAS COSMOSDB:
    /// - Partition Key: FieldId (distribui dados por campo)
    /// - Container: field-measurements
    /// - Queries otimizadas com partition key
    /// - Lazy initialization do client CosmosDB
    /// </summary>
    public class FieldMeasurementRepository : IFieldMeasurementRepository
    {
        private readonly ILogger<FieldMeasurementRepository> _logger;
        private Container? _container;
        private readonly string? _databaseId;
        private readonly string _containerId = "field-measurements";
        private readonly CosmosClient? _cosmosClient;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public FieldMeasurementRepository(
            IConfiguration configuration,
            ILogger<FieldMeasurementRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var cosmosConnectionString = configuration.GetConnectionString("AgroDbConnection");
            _databaseId = configuration["CosmosDb:DatabaseId"] ?? "AgroSolutionsDb";

            if (!string.IsNullOrEmpty(cosmosConnectionString))
            {
                _cosmosClient = new CosmosClient(cosmosConnectionString, new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    },
                    MaxRetryAttemptsOnRateLimitedRequests = 9,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
                });
            }
            else
            {
                _logger.LogWarning("CosmosDB connection string not configured. Repository operations will fail.");
            }
        }

        /// <summary>
        /// Garante que o container do CosmosDB está inicializado (lazy initialization).
        /// </summary>
        private async Task EnsureContainerAsync()
        {
            if (_container != null)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_container != null)
                    return;

                if (_cosmosClient == null || string.IsNullOrEmpty(_databaseId))
                    throw new InvalidOperationException("CosmosDB not properly configured.");

                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseId);
                
                // Cria container com partition key = /fieldId
                var containerResponse = await database.Database.CreateContainerIfNotExistsAsync(
                    _containerId,
                    partitionKeyPath: "/fieldId",
                    throughput: 400); // RU/s (pode ser ajustado conforme necessidade)

                _container = containerResponse.Container;

                _logger.LogInformation("CosmosDB container '{ContainerId}' initialized successfully.", _containerId);
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<FieldMeasurement> AddAsync(FieldMeasurement measurement)
        {
            await EnsureContainerAsync();

            try
            {
                // IMPORTANTE: partition key = FieldId
                var response = await _container!.CreateItemAsync(
                    measurement,
                    new PartitionKey(measurement.FieldId.ToString()));

                _logger.LogInformation(
                    "Measurement {MeasurementId} saved to CosmosDB. RU consumed: {RU}",
                    measurement.Id,
                    response.RequestCharge);

                return response.Resource;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error saving measurement {MeasurementId} to CosmosDB.", measurement.Id);
                throw;
            }
        }

        public async Task<FieldMeasurement?> GetByIdAsync(Guid id)
        {
            await EnsureContainerAsync();

            try
            {
                // Query sem partition key (menos eficiente, mas necessário quando não sabemos o FieldId)
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);

                var iterator = _container!.GetItemQueryIterator<FieldMeasurement>(query);
                
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }

                return null;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error retrieving measurement {MeasurementId} from CosmosDB.", id);
                throw;
            }
        }

        public async Task<IEnumerable<FieldMeasurement>> GetByFieldIdAsync(Guid fieldId)
        {
            await EnsureContainerAsync();

            try
            {
                // Query COM partition key (mais eficiente)
                var query = new QueryDefinition("SELECT * FROM c WHERE c.fieldId = @fieldId ORDER BY c.collectedAt DESC")
                    .WithParameter("@fieldId", fieldId);

                var iterator = _container!.GetItemQueryIterator<FieldMeasurement>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(fieldId.ToString())
                    });

                var results = new List<FieldMeasurement>();
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                return results;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error retrieving measurements for field {FieldId} from CosmosDB.", fieldId);
                throw;
            }
        }

        public async Task<IEnumerable<FieldMeasurement>> GetByFieldIdAndDateRangeAsync(
            Guid fieldId, 
            DateTime startDate, 
            DateTime endDate)
        {
            await EnsureContainerAsync();

            try
            {
                // Query COM partition key + filtro de data
                var query = new QueryDefinition("SELECT * FROM c " +
                                                "WHERE c.fieldId = @fieldId " +
                                                  "AND c.collectedAt >= @startDate " +
                                                  "AND c.collectedAt <= @endDate " +
                                                "ORDER BY c.collectedAt ASC")
                                .WithParameter("@fieldId", fieldId)
                                .WithParameter("@startDate", startDate)
                                .WithParameter("@endDate", endDate);

                var iterator = _container!.GetItemQueryIterator<FieldMeasurement>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(fieldId.ToString())
                    });

                var results = new List<FieldMeasurement>();
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                return results;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(
                    ex, 
                    "Error retrieving measurements for field {FieldId} in date range from CosmosDB.", 
                    fieldId);
                throw;
            }
        }

        public async Task<IEnumerable<FieldMeasurement>> GetPaginatedAsync(int skip, int take)
        {
            await EnsureContainerAsync();

            try
            {
                // Paginação cross-partition (menos eficiente, mas necessário para listagem geral)
                var query = new QueryDefinition($"SELECT * FROM c ORDER BY c.receivedAt DESC OFFSET {skip} LIMIT {take}");

                var iterator = _container!.GetItemQueryIterator<FieldMeasurement>(query);

                var results = new List<FieldMeasurement>();
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                return results;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error retrieving paginated measurements from CosmosDB.");
                throw;
            }
        }

        public async Task<int> CountAsync()
        {
            await EnsureContainerAsync();

            try
            {
                var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                var iterator = _container!.GetItemQueryIterator<int>(query);

                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }

                return 0;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error counting measurements in CosmosDB.");
                throw;
            }
        }
    }
}
