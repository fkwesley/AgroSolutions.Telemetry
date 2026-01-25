using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Infrastructure.Http.Clients
{
    /// <summary>
    /// Cliente HTTP para comunicação com a API externa de Games
    /// Responsabilidade: Fazer requisições HTTP e desserializar respostas
    /// </summary>
    public class GamesApiClient : IGamesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GamesApiClient> _logger;

        public GamesApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GamesApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configura HttpClient com base URL e headers de autenticação
            _httpClient.BaseAddress = new Uri(configuration["GamesAPI:EndPoint"] 
                ?? throw new InvalidOperationException("GamesAPI:EndPoint not configured"));

            var subscriptionKey = configuration["GamesAPI:OcpApimSubscriptionKey"];
            if (!string.IsNullOrEmpty(subscriptionKey))
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            
            var authToken = configuration["GamesAPI:Authorization"];
            if (!string.IsNullOrEmpty(authToken))
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }

        public async Task<Game?> GetGameByIdAsync(int id)
        {
            try
            {
                // Adiciona CorrelationId no header se disponível
                var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as Guid?;
                if (correlationId.HasValue)
                {
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                        "X-Correlation-Id", 
                        correlationId.Value.ToString());
                }

                var response = await _httpClient.GetAsync($"/games/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to get game {GameId} from Games API. Status: {StatusCode}, Reason: {Reason}",
                        id,
                        response.StatusCode,
                        response.ReasonPhrase);

                    return null;
                }

                var game = await response.Content.ReadFromJsonAsync<Game>();

                _logger.LogDebug("Successfully retrieved game {GameId} from Games API", id);

                return game;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while getting game {GameId} from Games API", id);
                throw;
            }
        }
    }
}
