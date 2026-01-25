using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.IntegrationTests.Common;

namespace Tests.IntegrationTests.Infrastructure.Repositories;

/// <summary>
/// Testes de integração básicos para OrderRepository.
/// Valida operações CRUD essenciais no banco de dados.
/// </summary>
public class BasicOrderRepositoryIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BasicOrderRepositoryIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddOrderAsync_ShouldPersistOrder()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = DatabaseSeeder.CreateTestOrder();

        // Act
        var result = await repository.AddOrderAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().BeGreaterThan(0);
        result.UserId.Should().Be(order.UserId);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnOrder()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = DatabaseSeeder.CreateTestOrder();
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetOrderByIdAsync(order.OrderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(order.OrderId);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnOrders()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        DatabaseSeeder.SeedTestData(context);

        // Act
        var result = await repository.GetAllOrdersAsync();

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateOrderAsync_ShouldModifyOrder()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = DatabaseSeeder.CreateTestOrder();
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        order.Status = OrderStatus.Paid;
        await repository.UpdateOrderAsync(order);

        // Assert
        var updatedOrder = await context.Orders.FindAsync(order.OrderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task DeleteOrderAsync_ShouldRemoveOrder()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = DatabaseSeeder.CreateTestOrder();
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var orderId = order.OrderId;

        // Act
        var result = await repository.DeleteOrderAsync(orderId);

        // Assert
        result.Should().BeTrue();
        var deletedOrder = await context.Orders.FindAsync(orderId);
        deletedOrder.Should().BeNull();
    }

    [Fact]
    public async Task GetOrdersPaginatedAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        DatabaseSeeder.SeedTestData(context);

        // Act
        var result = await repository.GetOrdersPaginatedAsync(skip: 0, take: 1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CountOrdersAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        DatabaseSeeder.SeedTestData(context);

        // Act
        var result = await repository.CountOrdersAsync();

        // Assert
        result.Should().BeGreaterThanOrEqualTo(2);
    }
}
