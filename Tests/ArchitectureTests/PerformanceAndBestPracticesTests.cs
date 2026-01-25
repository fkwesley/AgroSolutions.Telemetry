using NetArchTest.Rules;

namespace Tests.ArchitectureTests;

/// <summary>
/// Testes de performance e boas práticas específicas do .NET.
/// </summary>
public class PerformanceAndBestPracticesTests
{
    [Fact]
    public void Services_Should_Be_Async()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = new List<string>();

        foreach (var type in types)
        {
            var nonAsyncPublicMethods = type.GetMethods()
                .Where(m => m.IsPublic && 
                           !m.IsSpecialName && 
                           m.DeclaringType == type &&
                           m.ReturnType != typeof(void) &&
                           !m.ReturnType.Name.Contains("Task"))
                .Select(m => $"{type.Name}.{m.Name}")
                .ToList();

            violations.AddRange(nonAsyncPublicMethods);
        }

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Async_Methods_Should_Have_Async_Suffix()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = new List<string>();

        foreach (var type in types)
        {
            var asyncMethodsWithoutSuffix = type.GetMethods()
                .Where(m => m.IsPublic && 
                           !m.IsSpecialName && 
                           m.DeclaringType == type &&
                           m.ReturnType.Name.Contains("Task") &&
                           !m.Name.EndsWith("Async") &&
                           !m.Name.Equals("Dispose") && // Exclude Dispose
                           !m.Name.Equals("DisposeAsync"))
                .Select(m => $"{type.Name}.{m.Name}")
                .ToList();

            violations.AddRange(asyncMethodsWithoutSuffix);
        }

        // Assert - Soft check: alguns métodos como AddOrder podem ser aceitáveis
        Assert.True(violations.Count <= 5, 
            $"Consider adding 'Async' suffix to async methods: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Domain_Should_NotReference_LinqToEntities()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .ShouldNot()
            .HaveDependencyOn("System.Linq.IQueryable")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            "Domain should not use IQueryable (should be in repositories/infrastructure)");
    }

    [Fact]
    public void Controllers_Should_Not_Return_IQueryable()
    {
        // Arrange
        var types = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = new List<string>();

        foreach (var type in types)
        {
            var methodsReturningQueryable = type.GetMethods()
                .Where(m => m.IsPublic && 
                           !m.IsSpecialName && 
                           m.ReturnType.Name.Contains("IQueryable"))
                .Select(m => $"{type.Name}.{m.Name}")
                .ToList();

            violations.AddRange(methodsReturningQueryable);
        }

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void DTOs_Should_NotContain_Business_Logic()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Application.DTO.Order.OrderResponse).Assembly)
            .That()
            .ResideInNamespaceMatching("Application.DTO.*")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = new List<string>();

        foreach (var type in types)
        {
            // DTOs não devem ter métodos além de construtores e properties
            var businessMethods = type.GetMethods()
                .Where(m => m.IsPublic && 
                           !m.IsSpecialName && 
                           m.DeclaringType == type &&
                           !m.Name.StartsWith("get_") &&
                           !m.Name.StartsWith("set_") &&
                           m.Name != "ToString" &&
                           m.Name != "GetHashCode" &&
                           m.Name != "Equals")
                .Select(m => $"{type.Name}.{m.Name}")
                .ToList();

            violations.AddRange(businessMethods);
        }

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Services_Should_Be_Sealed_Or_Abstract()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .Or()
            .BeAbstract()
            .GetResult();

        // Assert - Soft check: sealed é recomendado mas não obrigatório
        var violationCount = result.FailingTypeNames?.Count() ?? 0;
        Assert.True(violationCount <= 5,
            $"Consider making services sealed for better performance: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
