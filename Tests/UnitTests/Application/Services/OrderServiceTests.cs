using Application.DTO.Common;
using Application.DTO.Game;
using Application.DTO.Order;
using Application.Exceptions;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IGameService> _mockGameService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContext;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IDomainEventDispatcher> _mockEventDispatcher;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockGameService = new Mock<IGameService>();
            _mockHttpContext = new Mock<IHttpContextAccessor>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockEventDispatcher = new Mock<IDomainEventDispatcher>();
            _mockLogger = new Mock<ILogger<OrderService>>();

            _orderService = new OrderService(
                _mockOrderRepository.Object,
                _mockGameService.Object,
                _mockHttpContext.Object,
                _mockScopeFactory.Object,
                _mockEventDispatcher.Object,
                _mockLogger.Object
            );
        }

        #region GetAllOrdersAsync Tests

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        public async Task GetAllOrdersAsync_WithDifferentCounts_ShouldReturnCorrectNumberOfOrders(int orderCount)
        {
            // Arrange
            var orders = CreateTestOrders(orderCount);
            _mockOrderRepository.Setup(r => r.GetAllOrdersAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetAllOrdersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(orderCount);
            _mockOrderRepository.Verify(r => r.GetAllOrdersAsync(), Times.Once);
        }

        #endregion

        #region GetOrdersPaginatedAsync Tests

        [Theory]
        [InlineData(1, 10, 5, 1, 1)]  // page, pageSize, totalCount, expectedTotalPages, expectedCurrentPage
        [InlineData(1, 10, 25, 3, 1)]
        [InlineData(2, 10, 25, 3, 2)]
        [InlineData(3, 10, 25, 3, 3)]
        [InlineData(1, 5, 12, 3, 1)]
        public async Task GetOrdersPaginatedAsync_WithDifferentPaginationParams_ShouldReturnCorrectMetadata(
            int page, int pageSize, int totalCount, int expectedTotalPages, int expectedCurrentPage)
        {
            // Arrange
            var orders = CreateTestOrders(pageSize);
            var paginationParams = new PaginationParameters { Page = page, PageSize = pageSize };
            var skip = (page - 1) * pageSize;
            
            _mockOrderRepository.Setup(r => r.GetOrdersPaginatedAsync(skip, pageSize))
                .ReturnsAsync(orders);
            _mockOrderRepository.Setup(r => r.CountOrdersAsync())
                .ReturnsAsync(totalCount);

            // Act
            var result = await _orderService.GetOrdersPaginatedAsync(paginationParams);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(totalCount);
            result.CurrentPage.Should().Be(expectedCurrentPage);
            result.PageSize.Should().Be(pageSize);
            result.TotalPages.Should().Be(expectedTotalPages);
        }

        [Theory]
        [InlineData(1, 10, 25, false, true)]   // page, pageSize, totalCount, expectedHasPrevious, expectedHasNext
        [InlineData(2, 10, 25, true, true)]
        [InlineData(3, 10, 25, true, false)]
        [InlineData(1, 10, 5, false, false)]
        public async Task GetOrdersPaginatedAsync_WithDifferentPages_ShouldHaveCorrectNavigationFlags(
            int page, int pageSize, int totalCount, bool expectedHasPrevious, bool expectedHasNext)
        {
            // Arrange
            var orders = CreateTestOrders(pageSize);
            var paginationParams = new PaginationParameters { Page = page, PageSize = pageSize };
            var skip = (page - 1) * pageSize;
            
            _mockOrderRepository.Setup(r => r.GetOrdersPaginatedAsync(skip, pageSize))
                .ReturnsAsync(orders);
            _mockOrderRepository.Setup(r => r.CountOrdersAsync())
                .ReturnsAsync(totalCount);

            // Act
            var result = await _orderService.GetOrdersPaginatedAsync(paginationParams);

            // Assert
            result.HasPrevious.Should().Be(expectedHasPrevious);
            result.HasNext.Should().Be(expectedHasNext);
        }

        #endregion

        #region GetOrderByIdAsync Tests

        [Theory]
        [InlineData(1, "user-123")]
        [InlineData(5, "user-456")]
        [InlineData(100, "admin-user")]
        public async Task GetOrderByIdAsync_WithValidId_ShouldReturnOrder(int orderId, string userId)
        {
            // Arrange
            var order = CreateTestOrder(orderId, userId);
            _mockOrderRepository.Setup(r => r.GetOrderByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderByIdAsync(orderId);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().Be(orderId);
            result.UserId.Should().Be(userId.ToUpper());
        }

        [Theory]
        [InlineData(999)]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task GetOrderByIdAsync_WithInvalidId_ShouldThrowException(int invalidId)
        {
            // Arrange
            _mockOrderRepository.Setup(r => r.GetOrderByIdAsync(invalidId))
                .ThrowsAsync(new KeyNotFoundException("Order not found"));

            // Act
            Func<Task> act = async () => await _orderService.GetOrderByIdAsync(invalidId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Order not found");
        }

        #endregion

        #region AddOrder Tests

        [Fact]
        public async Task AddOrder_WithValidData_ShouldCreateOrder()
        {
            // Arrange
            var addOrderRequest = CreateValidAddOrderRequest();
            var gameData = CreateTestGame(1, "Test Game", 59.99);
            
            _mockOrderRepository.Setup(r => r.GetAllOrdersAsync())
                .ReturnsAsync(new List<Order>());
            _mockGameService.Setup(g => g.GetGameByIdAsync(1))
                .Returns(gameData);
            _mockOrderRepository.Setup(r => r.AddOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order o) =>
                {
                    o.OrderId = 1;
                    return o;
                });
            _mockEventDispatcher.Setup(d => d.ProcessAsync(It.IsAny<IEnumerable<IDomainEvent>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.AddOrder(addOrderRequest);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().Be(1);
            result.UserId.Should().Be("USER-123");
            _mockOrderRepository.Verify(r => r.AddOrderAsync(It.IsAny<Order>()), Times.Once);
            _mockEventDispatcher.Verify(d => d.ProcessAsync(It.IsAny<IEnumerable<IDomainEvent>>()), Times.Once);
        }

        [Theory]
        [InlineData(OrderStatus.PendingPayment)]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Processing)]
        public async Task AddOrder_WithDuplicateActiveGame_InDifferentStatuses_ShouldThrowValidationException(OrderStatus status)
        {
            // Arrange
            var addOrderRequest = CreateValidAddOrderRequest();
            var existingOrders = new List<Order>
            {
                CreateTestOrder(1, "user-123", status, new List<int> { 1 })
            };
            
            _mockOrderRepository.Setup(r => r.GetAllOrdersAsync())
                .ReturnsAsync(existingOrders);

            // Act
            Func<Task> act = async () => await _orderService.AddOrder(addOrderRequest);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*already an active order*");
        }

        [Fact]
        public async Task AddOrder_WithNonExistentGame_ShouldThrowValidationException()
        {
            // Arrange
            var addOrderRequest = CreateValidAddOrderRequest();
            
            _mockOrderRepository.Setup(r => r.GetAllOrdersAsync())
                .ReturnsAsync(new List<Order>());
            _mockGameService.Setup(g => g.GetGameByIdAsync(1))
                .Returns((GameResponse?)null!);

            // Act
            Func<Task> act = async () => await _orderService.AddOrder(addOrderRequest);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*is not available*");
        }

        [Theory]
        [InlineData(OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Released)]
        [InlineData(OrderStatus.Refunded)]
        public async Task AddOrder_WithInactiveOrder_ShouldAllowCreation(OrderStatus inactiveStatus)
        {
            // Arrange
            var addOrderRequest = CreateValidAddOrderRequest();
            var existingOrders = new List<Order>
            {
                CreateTestOrder(1, "user-123", inactiveStatus, new List<int> { 1 })
            };
            var gameData = CreateTestGame(1, "Test Game", 59.99);
            
            _mockOrderRepository.Setup(r => r.GetAllOrdersAsync())
                .ReturnsAsync(existingOrders);
            _mockGameService.Setup(g => g.GetGameByIdAsync(1))
                .Returns(gameData);
            _mockOrderRepository.Setup(r => r.AddOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order o) =>
                {
                    o.OrderId = 2;
                    return o;
                });
            _mockEventDispatcher.Setup(d => d.ProcessAsync(It.IsAny<IEnumerable<IDomainEvent>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.AddOrder(addOrderRequest);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().Be(2);
        }

        #endregion

        #region UpdateOrder Tests

        [Theory]
        [InlineData(OrderStatus.PendingPayment)]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Processing)]
        [InlineData(OrderStatus.Released)]
        public async Task UpdateOrder_WithDifferentStatuses_ShouldUpdateOrderCorrectly(OrderStatus newStatus)
        {
            // Arrange
            var updateRequest = new UpdateOrderRequest()
            {
                OrderId = 1,
                UserId = "user-123",
                Status = newStatus
            };

            var updatedOrder = CreateTestOrder(1, "user-123", newStatus);
            
            _mockOrderRepository.Setup(r => r.UpdateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync(updatedOrder);
            _mockEventDispatcher.Setup(d => d.ProcessAsync(It.IsAny<IEnumerable<IDomainEvent>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.UpdateOrder(updateRequest);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(newStatus);
            _mockOrderRepository.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Once);
            _mockEventDispatcher.Verify(d => d.ProcessAsync(It.IsAny<IEnumerable<IDomainEvent>>()), Times.Once);
        }

        #endregion

        #region DeleteOrderAsync Tests

        [Theory]
        [InlineData(1, true)]
        [InlineData(5, true)]
        [InlineData(100, true)]
        public async Task DeleteOrderAsync_WithValidId_ShouldReturnTrue(int orderId, bool expectedResult)
        {
            // Arrange
            _mockOrderRepository.Setup(r => r.DeleteOrderAsync(orderId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _orderService.DeleteOrderAsync(orderId);

            // Assert
            result.Should().Be(expectedResult);
            _mockOrderRepository.Verify(r => r.DeleteOrderAsync(orderId), Times.Once);
        }

        [Theory]
        [InlineData(999, false)]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        public async Task DeleteOrderAsync_WithInvalidId_ShouldReturnFalse(int invalidId, bool expectedResult)
        {
            // Arrange
            _mockOrderRepository.Setup(r => r.DeleteOrderAsync(invalidId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _orderService.DeleteOrderAsync(invalidId);

            // Assert
            result.Should().Be(expectedResult);
        }

        #endregion

        #region Helper Methods

        private List<Order> CreateTestOrders(int count)
        {
            var orders = new List<Order>();
            for (int i = 1; i <= count; i++)
            {
                orders.Add(CreateTestOrder(i, $"user-{i}"));
            }
            return orders;
        }

        private Order CreateTestOrder(int orderId, string userId, OrderStatus status = OrderStatus.PendingPayment, List<int>? gameIds = null)
        {
            var order = new Order
            {
                OrderId = orderId,
                UserId = userId,
                UserEmail = $"{userId}@test.com",
                Status = status,
                PaymentMethod = PaymentMethod.Pix,
                ListOfGames = new List<Game>()
            };

            if (gameIds != null)
            {
                foreach (var gameId in gameIds)
                {
                    order.ListOfGames.Add(new Game
                    {
                        GameId = gameId,
                        Name = $"Game {gameId}",
                        Price = 59.99
                    });
                }
            }

            return order;
        }

        private GameResponse CreateTestGame(int gameId, string name, double price)
        {
            return new GameResponse
            {
                GameId = gameId,
                Name = name,
                Price = price
            };
        }

        private AddOrderRequest CreateValidAddOrderRequest()
        {
            return new AddOrderRequest
            {
                UserId = "user-123",
                Email = "user@test.com",
                PaymentMethodDetails = null,
                PaymentMethod = PaymentMethod.Pix,
                ListOfGames = new List<int> { 1}
            };
        }

        #endregion
    }
}
