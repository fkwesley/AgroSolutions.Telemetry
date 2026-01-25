using Domain.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// Client para comunicação com a Games API externa
    /// </summary>
    public interface IGamesApiClient
    {
        /// <summary>
        /// Obtém um jogo por ID da API externa
        /// </summary>
        /// <param name="id">ID do jogo</param>
        /// <returns>Jogo encontrado ou null se não existir</returns>
        Task<Game?> GetGameByIdAsync(int id);
    }
}
