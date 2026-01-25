using Domain.Entities;

namespace Domain.Repositories
{
    // #SOLID - Dependency Inversion Principle (DIP)
    // A camada de domínio define a interface do repositório (abstração).
    // A infraestrutura implementa essa abstração, invertendo a dependência tradicional.
    // Domain não depende de Infrastructure; Infrastructure depende de Domain.
    
    // #SOLID - Interface Segregation Principle (ISP)
    // Interface focada apenas em operações de persistência de Order.
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        
        /// <summary>
        /// Retorna pedidos paginados.
        /// </summary>
        /// <param name="skip">Quantos itens pular</param>
        /// <param name="take">Quantos itens retornar</param>
        /// <returns>Lista paginada de pedidos</returns>
        Task<IEnumerable<Order>> GetOrdersPaginatedAsync(int skip, int take);
        
        /// <summary>
        /// Conta o total de pedidos no banco.
        /// </summary>
        /// <returns>Total de pedidos</returns>
        Task<int> CountOrdersAsync();
        
        Task<Order> GetOrderByIdAsync(int id);
        Task<Order> AddOrderAsync(Order Order);
        Task<Order> UpdateOrderAsync(Order Order);
        Task<bool> DeleteOrderAsync(int id);
    }
}
