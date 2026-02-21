using Application.DTO.Health;
using Application.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.HealthCheck
{
    /// <summary>
    /// Health check para Azure CosmosDB.
    /// Verifica conectividade e latência da base de dados.
    /// </summary>
    public class CosmosDBHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CosmosDBHealthCheck> _logger;

        public string ComponentName => "CosmosDB";
        public bool IsCritical => true; // CosmosDB é crítico - API não funciona sem ele

        public CosmosDBHealthCheck(
            IConfiguration configuration,
            ILogger<CosmosDBHealthCheck> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentHealth> CheckHealthAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            CosmosClient? cosmosClient = null;

            try
            {
                _logger.LogInformation("Starting CosmosDB health check");

                var connectionString = _configuration.GetConnectionString("TelemetryDbConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogWarning("CosmosDB connection string not configured");
                    return new ComponentHealth
                    {
                        Status = "Unhealthy",
                        Description = "CosmosDB connection string not configured",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                _logger.LogInformation("Creating CosmosDB client with timeout: 5s");

                cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
                {
                    RequestTimeout = TimeSpan.FromSeconds(5),
                    OpenTcpConnectionTimeout = TimeSpan.FromSeconds(5),
                    MaxRetryAttemptsOnRateLimitedRequests = 0,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.Zero
                });

                _logger.LogInformation("Reading CosmosDB account properties from endpoint: {Endpoint}", cosmosClient.Endpoint.Host);

                // Verifica a conta do CosmosDB (não depende de database existir)
                var accountProperties = await cosmosClient.ReadAccountAsync();

                stopwatch.Stop();

                var status = stopwatch.ElapsedMilliseconds switch
                {
                    < 1000 => "Healthy",
                    < 2000 => "Degraded",
                    _ => "Unhealthy"
                };

                _logger.LogInformation(
                    "CosmosDB health check successful: Status={Status}, ResponseTime={ResponseTime}ms, Consistency={Consistency}",
                    status,
                    stopwatch.ElapsedMilliseconds,
                    accountProperties.Consistency.DefaultConsistencyLevel);

                return new ComponentHealth
                {
                    Status = status,
                    Description = $"CosmosDB account accessible. Endpoint: {cosmosClient.Endpoint.Host}, Consistency: {accountProperties.Consistency.DefaultConsistencyLevel}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (CosmosException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, 
                    "CosmosDB health check failed - CosmosException: StatusCode={StatusCode}, Message={Message}, ActivityId={ActivityId}",
                    ex.StatusCode, 
                    ex.Message,
                    ex.ActivityId);

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    Description = $"CosmosDB error: {ex.StatusCode} - {ex.Message}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, 
                    "CosmosDB health check failed - Unexpected error: {ExceptionType} - {Message}",
                    ex.GetType().Name,
                    ex.Message);

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    Description = $"CosmosDB connection failed: {ex.Message}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            finally
            {
                if (cosmosClient != null)
                {
                    _logger.LogDebug("Disposing CosmosDB client");
                    cosmosClient.Dispose();
                }
            }
        }
    }
}
