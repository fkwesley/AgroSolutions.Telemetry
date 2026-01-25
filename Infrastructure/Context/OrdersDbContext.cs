using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply configurations from the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

            // Alternatively, you can apply configurations explicitly if needed
            //modelBuilder.ApplyConfiguration(new GameConfiguration());
            //modelBuilder.ApplyConfiguration(new UserConfiguration());
        }
    }
}
