using NetArchTest.Rules;

namespace Tests.ArchitectureTests;

/// <summary>
/// Testes que garantem princípios SOLID e boas práticas de design.
/// </summary>
public class SolidPrinciplesTests
{
    #region Single Responsibility Principle (SRP)

    [Fact]
    public void Services_Should_NotBe_Too_Large()
    {
        // Arrange
        var maxMethodsPerService = 15;
        
        // Act
        var types = Types.InAssembly(typeof(Application.Interfaces.IOrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = types
            .Where(t => t.GetMethods().Length > maxMethodsPerService)
            .Select(t => $"{t.Name} has {t.GetMethods().Length} methods")
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    #endregion

    #region Dependency Inversion Principle (DIP)

    [Fact]
    public void Services_Should_DependOn_Interfaces_Not_Implementations()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .ShouldNot()
            .HaveDependencyOn("Infrastructure.Repositories")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Services should depend on interfaces (in Application.Interfaces), not concrete implementations. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_DependOn_Interfaces()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Application.Services")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            "Controllers should depend on service interfaces (Application.Interfaces), not concrete services");
    }

    #endregion

    #region Interface Segregation Principle (ISP)

    [Fact]
    public void Interfaces_Should_NotBe_Too_Large()
    {
        // Arrange
        var maxMethodsPerInterface = 12;
        
        // Act
        var types = Types.InAssembly(typeof(Application.Interfaces.IOrderService).Assembly)
            .That()
            .AreInterfaces()
            .GetTypes();

        var violations = types
            .Where(t => t.GetMethods().Length > maxMethodsPerInterface)
            .Select(t => $"{t.Name} has {t.GetMethods().Length} methods")
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    #endregion

    #region Open/Closed Principle (OCP) - Design Checks

    [Fact]
    public void Domain_Entities_Should_NotBe_Sealed()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .That()
            .ResideInNamespace("Domain.Entities")
            .And()
            .AreClasses()
            .And()
            .DoNotHaveNameMatching(".*BaseEntity")
            .ShouldNot()
            .BeSealed()
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            "Domain entities should not be sealed to allow extension (Open/Closed Principle)");
    }

    #endregion
}
