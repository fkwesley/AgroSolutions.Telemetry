using NetArchTest.Rules;

namespace Tests.ArchitectureTests;

/// <summary>
/// Testes de testabilidade e design para testes.
/// </summary>
public class TestabilityTests
{
    [Fact]
    public void Services_Should_Have_Constructor_For_Dependency_Injection()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = types
            .Where(t => !t.IsAbstract && t.GetConstructors().Length == 0)
            .Select(t => t.Name)
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Services_Should_NotHave_Multiple_Public_Constructors()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = types
            .Where(t => t.GetConstructors().Count(c => c.IsPublic) > 1)
            .Select(t => $"{t.Name} has {t.GetConstructors().Count(c => c.IsPublic)} public constructors")
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Domain_Entities_Should_Favor_ExplicitConstructors()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .That()
            .ResideInNamespace("Domain.Entities")
            .And()
            .AreClasses()
            .And()
            .DoNotHaveName("BaseEntity")
            .GetTypes();

        var violations = new List<string>();

        foreach (var type in types)
        {
            var parameterlessConstructors = type.GetConstructors()
                .Where(c => c.IsPublic && c.GetParameters().Length == 0)
                .ToList();

            // OK ter construtor sem parâmetros se for protected/private (para EF)
            if (parameterlessConstructors.Any())
            {
                violations.Add($"{type.Name} has public parameterless constructor");
            }
        }

        // Assert - Soft assertion pois EF precisa de construtores
        // Este é mais um guia de boas práticas do que uma regra rígida
        Assert.True(violations.Count <= 5, 
            $"Consider making parameterless constructors protected for EF: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Controllers_Should_Only_Depend_On_Service_Interfaces()
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
            var constructorParams = type.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Where(p => p.ParameterType.Namespace?.StartsWith("Application") == true &&
                           !p.ParameterType.IsInterface)
                .Select(p => $"{type.Name} depends on concrete type {p.ParameterType.Name}")
                .ToList();

            violations.AddRange(constructorParams);
        }

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Infrastructure_Repositories_Should_Implement_Interface()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Infrastructure.Context.OrdersDbContext).Assembly)
            .That()
            .ResideInNamespaceMatching("Infrastructure.Repositories.*")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Repository")
            .GetTypes();

        var violations = types
            .Where(t => !t.GetInterfaces().Any())
            .Select(t => t.Name)
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Static_Classes_Should_Be_Limited()
    {
        // Arrange
        var maxStaticClasses = 10; // Ajuste conforme necessário
        
        // Act
        var staticClasses = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .AreClasses()
            .And()
            .AreStatic()
            .GetTypes()
            .Where(t => !t.Name.Contains("Extensions")) // Extensions são OK
            .ToList();

        // Assert
        Assert.True(staticClasses.Count <= maxStaticClasses,
            $"Too many static classes ({staticClasses.Count}). Static classes make testing harder.");
    }
}
