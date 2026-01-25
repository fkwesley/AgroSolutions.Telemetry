using Application.DTO.Game;
using Application.Interfaces;
using FCG.Application.Mappings;

namespace FCG.Application.Services
{
    /// <summary>
    /// Serviço de domínio para operações relacionadas a Games
    /// Contém lógica de negócio e usa IGamesApiClient para comunicação
    /// </summary>
    public class GameService : IGameService
    {
        private readonly IGamesApiClient _gamesApiClient;

        public GameService(IGamesApiClient gamesApiClient)
        {
            _gamesApiClient = gamesApiClient 
                ?? throw new ArgumentNullException(nameof(gamesApiClient));
        }

        public GameResponse GetGameByIdAsync(int id)
        {
            // Validação de negócio (exemplo)
            if (id <= 0)
                throw new ArgumentException("Game ID must be greater than zero", nameof(id));

            // Obtém game da API externa via client
            var game = _gamesApiClient.GetGameByIdAsync(id).GetAwaiter().GetResult();

            // Regra de negócio: Se não encontrar, lança exceção
            if (game == null)
                throw new KeyNotFoundException($"Game with ID {id} not found");

            // Mapeia para DTO de resposta
            var response = game.ToResponse();
            
            if (response == null)
                throw new InvalidOperationException("Failed to map game to response");
                
            return response;
        }
    }
}
