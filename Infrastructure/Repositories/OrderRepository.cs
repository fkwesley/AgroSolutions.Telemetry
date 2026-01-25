using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    // #SOLID - Single Responsibility Principle (SRP)
    // OrderRepository tem uma única responsabilidade: gerenciar a persistência de pedidos.
    // Não contém lógica de negócio, apenas operações de banco de dados.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Implementa a interface IOrderRepository definida no domínio.
    // A infraestrutura depende do domínio, não o contrário (inversão de dependência).
    
    // #SOLID - Liskov Substitution Principle (LSP)
    // OrderRepository pode ser substituído por qualquer outra implementação de IOrderRepository
    // (ex: InMemoryOrderRepository, MongoOrderRepository) sem quebrar o código cliente.
    public class OrderRepository : IOrderRepository
    {
        private readonly OrdersDbContext _context;

        public OrderRepository(OrdersDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
           return await _context.Orders
                            .Include(o => o.ListOfGames)
                            .ToListAsync();
        }

        /// <summary>
        /// Retorna pedidos paginados usando Skip e Take.
        /// IMPORTANTE: Skip/Take são traduzidos para OFFSET/FETCH no SQL Server.
        /// </summary>
        public async Task<IEnumerable<Order>> GetOrdersPaginatedAsync(int skip, int take)
        {
            return await _context.Orders
                .Include(o => o.ListOfGames)
                .OrderByDescending(o => o.CreatedAt)  // Ordenação necessária para paginação
                .Skip(skip)  // Pula os primeiros N registros
                .Take(take)  // Retorna os próximos M registros
                .ToListAsync();
        }

        /// <summary>
        /// Conta total de pedidos.
        /// Usa CountAsync para performance (não carrega dados na memória).
        /// </summary>
        public async Task<int> CountOrdersAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                            .Include(o => o.ListOfGames)
                            .FirstOrDefaultAsync(o => o.OrderId == id)
                ?? throw new KeyNotFoundException($"Order with ID {id} not found.");
        }

        public async Task<Order> AddOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            var existingOrder = await GetOrderByIdAsync(order.OrderId);

            if (existingOrder != null) {
                existingOrder.Status = order.Status;
                existingOrder.UpdatedAt = DateTime.UtcNow;

                _context.Orders.Update(existingOrder);
                await _context.SaveChangesAsync();
            }
            else
                throw new KeyNotFoundException($"Order with ID {order.OrderId} not found.");

            return existingOrder;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var game = await GetOrderByIdAsync(id);

            if (game != null)
            {
                _context.Games.RemoveRange(game.ListOfGames); // Remove associated games first
                _context.Orders.Remove(game);
                await _context.SaveChangesAsync();
                return true;
            }
            else
                throw new KeyNotFoundException($"Order with ID {id} not found.");
        }

    }
}
