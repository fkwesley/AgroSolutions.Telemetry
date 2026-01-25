using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;

namespace Tests.IntegrationTests.Common;

/// <summary>
/// Helper para popular o banco de dados de teste com dados iniciais.
/// </summary>
public static class DatabaseSeeder
{
    public static void SeedTestData(OrdersDbContext context)
    {
        // Limpa dados existentes
        context.Orders.RemoveRange(context.Orders);
        context.Games.RemoveRange(context.Games);
        context.SaveChanges();

        var games = new List<Game>
        {
            new Game
            {
                GameId = 1,
                Name = "Test Game 1",
                Price = 59.99
            },
            new Game
            {
                GameId = 2,
                Name = "Test Game 2",
                Price = 39.99
            },
            new Game
            {
                GameId = 3,
                Name = "Test Game 3",
                Price = 29.99
            }
        };

        // Seed Orders
        var order1 = new Order
        {
            OrderId = 1,
            UserId = "test-user-1",
            UserEmail = "user1@test.com",
            Status = OrderStatus.PendingPayment,
            PaymentMethod = PaymentMethod.Pix,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ListOfGames = games
        };

        var order2 = new Order
        {
            OrderId = 2,
            UserId = "test-user-2",
            UserEmail = "user2@test.com",
            Status = OrderStatus.Paid,
            PaymentMethod = PaymentMethod.Pix,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ListOfGames = games
        };

        context.Orders.AddRange(order1, order2);
        context.SaveChanges();
    }

    public static Order CreateTestOrder(string userId = "test-user", string email = "test@example.com")
    {
        return new Order
        {
            UserId = userId,
            UserEmail = email,
            Status = OrderStatus.PendingPayment,
            PaymentMethod = PaymentMethod.Pix,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Game CreateTestGame(int id = 1, string name = "Test Game", decimal price = 49.99m)
    {
        return new Game
        {
            GameId = id,
            Name = name,
            Price = (double)price
        };
    }
}
