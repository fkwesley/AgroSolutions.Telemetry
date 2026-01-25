using Application.DTO.Common;
using Application.DTO.Order;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    // #SOLID - Single Responsibility Principle (SRP)
    // Esta classe tem uma única responsabilidade: gerenciar a lógica de negócio de pedidos.
    // Ela não se preocupa com detalhes de infraestrutura (DB, mensageria, logs), delegando para outras abstrações.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // OrderService depende de ABSTRAÇÕES (interfaces) e não de implementações concretas.
    // Todas as dependências são injetadas via construtor (IOrderRepository, IGameService, IDomainEventDispatcher, etc.)
    // Isso permite trocar implementações sem alterar esta classe.
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IGameService _gameService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly ILogger<OrderService> _logger;

        // #SOLID - Dependency Inversion Principle (DIP)
        // Constructor Injection: Todas as dependências são abstrações (interfaces).
        // Isso facilita testes unitários (mocks) e desacoplamento da implementação.
        public OrderService(
                IOrderRepository orderRepository,
                IGameService gameService,
                IHttpContextAccessor httpContext,
                IServiceScopeFactory scopeFactory,
                IDomainEventDispatcher eventDispatcher,
                ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository
                ?? throw new ArgumentNullException(nameof(orderRepository));
            _gameService = gameService;
            _httpContext = httpContext;
            _scopeFactory = scopeFactory;
            _eventDispatcher = eventDispatcher;
            _logger = logger;
        }

        public async Task<IEnumerable<OrderResponse>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();

            _logger.LogInformation("Retrieved {OrderCount} orders", orders.Count());

            return orders.Select(order => order.ToResponse()).ToList();
        }

        public async Task<PagedResponse<OrderResponse>> GetOrdersPaginatedAsync(PaginationParameters paginationParams)
        {
            // 1. Buscar dados paginados
            var orders = await _orderRepository.GetOrdersPaginatedAsync(
                paginationParams.Skip,  // Quantos pular: (page-1) * pageSize
                paginationParams.Take   // Quantos retornar: pageSize
            );

            // 2. Contar total (executa query separada: SELECT COUNT(*))
            var totalCount = await _orderRepository.CountOrdersAsync();

            // 3. Mapear para DTOs
            var orderResponses = orders.Select(order => order.ToResponse()).ToList();

            // 4. Criar resposta paginada com metadados
            var pagedResponse = new PagedResponse<OrderResponse>(
                data: orderResponses,
                totalCount: totalCount,
                currentPage: paginationParams.Page,
                pageSize: paginationParams.PageSize
            );

            // 5. SERILOG: Log estruturado com propriedades
            _logger.LogInformation(
                "Retrieved paginated orders: Page {Page}/{TotalPages}, PageSize: {PageSize}, Total: {TotalCount}",
                paginationParams.Page,
                pagedResponse.TotalPages,
                paginationParams.PageSize,
                totalCount);

            return pagedResponse;
        }

        public async Task<OrderResponse> GetOrderByIdAsync(int id)
        {
            var orderFound = await _orderRepository.GetOrderByIdAsync(id);

            return orderFound.ToResponse();
        }

        public async Task<OrderResponse> AddOrder(AddOrderRequest order)
        {
            //getting user orders
            var userOrders = await _orderRepository.GetAllOrdersAsync();
            var activeUserOrders = userOrders.Where(o => o.UserId.Equals(order.UserId, StringComparison.OrdinalIgnoreCase));

            //verifying if there is any active order with some of the games requested
            if (activeUserOrders.Any(o => o.ListOfGames.Any(g => order.ListOfGames.Contains(g.GameId))
                                 && o.Status != OrderStatus.Cancelled
                                 && o.Status != OrderStatus.Released
                                 && o.Status != OrderStatus.Refunded))
                throw new ValidationException(string.Format("There is already an active order for the user {0} with one or more of the games requested.", order.UserId.ToUpper()));

            var orderEntity = order.ToEntity();

            //verifying if all games exists and getting their prices
            foreach (var game in orderEntity.ListOfGames)
            {
                var existingGame = _gameService.GetGameByIdAsync(game.GameId);

                if (existingGame == null)
                    throw new ValidationException(string.Format("Game with id {0} is not available.", game.GameId));

                game.Price = existingGame.Price;
                game.Name = existingGame.Name;
            }

            var orderAdded = await _orderRepository.AddOrderAsync(orderEntity);
            _logger.LogInformation("Order {OrderId} created for user {UserId}.", orderAdded.OrderId, orderAdded.UserId);

            // #SOLID - Single Responsibility Principle (SRP) + Open/Closed Principle (OCP)
            // O OrderService não sabe quais handlers vão processar os eventos.
            // Novos handlers podem ser adicionados sem modificar esta classe.
            // Processa eventos (OrderId já foi gerado e MarkAsCreated já foi chamado no Repository)
            await _eventDispatcher.ProcessAsync(orderAdded.DomainEvents);
            
            // Limpa eventos após processar
            orderAdded.ClearDomainEvents();

            return orderAdded.ToResponse();
        }

        public async Task<OrderResponse> UpdateOrder(UpdateOrderRequest order)
        {
            var orderEntity = order.ToEntity();
            var orderUpdated = await _orderRepository.UpdateOrderAsync(orderEntity);
            _logger.LogInformation("Order {OrderId} updated. New status: {Status}.", orderUpdated.OrderId, orderUpdated.Status);

            // Processa eventos (eventos disparados no setter de Status)
            await _eventDispatcher.ProcessAsync(orderUpdated.DomainEvents);
            
            // Limpa eventos após processar
            orderUpdated.ClearDomainEvents();

            return orderUpdated.ToResponse();
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var deleteStatus = await _orderRepository.DeleteOrderAsync(id);
            _logger.LogInformation("Order {OrderId} deleted.", id);

            return deleteStatus;
        }

    }
}
