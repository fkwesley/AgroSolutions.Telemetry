using Application.DTO.Health;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.Services.HealthCheck
{
    /// <summary>
    /// Games API Health Check Implementation
    /// #SOLID - Single Responsibility Principle (SRP)
    /// #SOLID - Dependency Inversion Principle (DIP)
    /// Reutiliza IGamesApiClient existente em vez de duplicar código HTTP
    /// </summary>
    public class GamesApiHealthCheck : IHealthCheck
    {
        private readonly IGamesApiClient _gamesApiClient;
        private readonly ILogger<GamesApiHealthCheck> _logger;

        public string ComponentName => "GamesAPI";
        public bool IsCritical => false; // Não-crítico - API funciona sem Games API

        public GamesApiHealthCheck(
            IGamesApiClient gamesApiClient,
            ILogger<GamesApiHealthCheck> logger)
        {
            _gamesApiClient = gamesApiClient ?? throw new ArgumentNullException(nameof(gamesApiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ComponentHealth> CheckHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Reutiliza o client existente para fazer uma chamada leve
                // Tenta buscar um game com ID 1 (ou qualquer ID conhecido)
                var game = await _gamesApiClient.GetGameByIdAsync(1);
                stopwatch.Stop();

                // Se conseguiu fazer a chamada (mesmo que retorne null), a API está respondendo
                return new ComponentHealth
                {
                    Status = "Healthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = "Games API is responding"
                };
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Games API health check failed - HTTP error");

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = $"Games API unreachable: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Games API health check timeout");

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = "Games API timeout"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Games API health check failed");

                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Description = $"Games API error: {ex.Message}"
                };
            }
        }
    }
}
