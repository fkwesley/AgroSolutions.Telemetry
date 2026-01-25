using Application.DTO.Common;
using Application.DTO.Order;

namespace Application.Interfaces
{
    // #SOLID - Interface Segregation Principle (ISP)
    // Esta interface define apenas os métodos relacionados a operações de pedidos.
    // Clientes que dependem dela não são forçados a depender de métodos que não usam.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Esta interface permite que camadas superiores (API) dependam de abstração,
    // não da implementação concreta (OrderService).
    public interface IOrderService
    {
        Task<IEnumerable<OrderResponse>> GetAllOrdersAsync();
        
        /// <summary>
        /// Retorna pedidos paginados com metadados.
        /// </summary>
        /// <param name="paginationParams">Parâmetros de paginação</param>
        /// <returns>Resposta paginada com dados e metadados</returns>
        Task<PagedResponse<OrderResponse>> GetOrdersPaginatedAsync(PaginationParameters paginationParams);
        
        Task<OrderResponse> GetOrderByIdAsync(int id);
        Task<OrderResponse> AddOrder(AddOrderRequest game);
        Task<OrderResponse> UpdateOrder(UpdateOrderRequest game);
        Task<bool> DeleteOrderAsync(int id);
    }
}
