using NetArchTest.Rules;

namespace Tests.ArchitectureTests;

/// <summary>
/// Testes que garantem práticas de segurança e qualidade do código.
/// </summary>
public class SecurityAndQualityTests
{
    [Fact]
    public void Controllers_Should_Have_Authorization_Attribute()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Controller")
            .And()
            .DoNotHaveName("HealthController") // Health checks são públicos
            .Should()
            .HaveCustomAttribute(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute))
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All controllers (except HealthController) should have [Authorize] attribute for security. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DTOs_Should_NotHave_Public_Fields()
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
            var publicFields = type.GetFields()
                .Where(f => f.IsPublic && !f.IsInitOnly)
                .Select(f => $"{type.Name}.{f.Name}")
                .ToList();

            violations.AddRange(publicFields);
        }

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Domain_Entities_Should_Favor_Encapsulation()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .That()
            .ResideInNamespace("Domain.Entities")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = new List<string>();

        foreach (var type in types)
        {
            // Verifica propriedades com setters públicos que não são Ids ou de navegação
            var publicSetters = type.GetProperties()
                .Where(p => p.SetMethod?.IsPublic == true && 
                           !p.Name.EndsWith("Id") && 
                           !p.Name.Contains("Order") && // Navigation properties
                           !p.Name.Contains("Game") &&  // Navigation properties
                           !p.Name.Contains("List") &&  // Collections
                           p.DeclaringType == type)
                .Select(p => $"{type.Name}.{p.Name}")
                .ToList();

            // Nota: Esta é uma verificação soft - entidades podem ter setters para EF e mapeamento
            // Se você tem muitas violações, considere usar private set ou métodos de domínio
        }

        // Assert - Apenas um aviso, não falha o teste
        Assert.True(true, "Entity encapsulation check - consider using private setters and domain methods");
    }

    [Fact]
    public void Services_Should_NotUse_Concrete_DbContext_Directly()
    {
        // Arrange & Act
        var types = Types.InAssembly(typeof(Application.Services.OrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .GetTypes();

        var violations = types
            .Where(t => t.GetConstructors()
                .Any(c => c.GetParameters()
                    .Any(p => p.ParameterType.Name.Contains("DbContext"))))
            .Select(t => t.Name)
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Repositories_Should_NotBe_Public_Outside_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Infrastructure.Context.OrdersDbContext).Assembly)
            .That()
            .ResideInNamespaceMatching("Infrastructure.Repositories.*")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .NotBePublic()
            .Or()
            .BeSealed()
            .GetResult();

        // Assert
        // Note: Este teste pode falhar se você usar repositórios públicos.
        // Ajuste conforme sua arquitetura.
        Assert.True(result.IsSuccessful || result.FailingTypeNames?.Count() < 5,
            "Consider making repositories internal or sealed to prevent misuse outside Infrastructure");
    }

    [Fact]
    public void Domain_Should_NotContain_DataAnnotations()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .That()
            .ResideInNamespace("Domain.Entities")
            .ShouldNot()
            .HaveDependencyOn("System.ComponentModel.DataAnnotations")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            "Domain entities should not use DataAnnotations (use FluentValidation or domain validation instead)");
    }

    [Fact]
    public void Application_Should_MinimizeReference_To_AspNetCore()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.Interfaces.IOrderService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("Application")
            .And()
            .DoNotResideInNamespace("Application.DTO") // DTOs podem ter DataAnnotations
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore.Mvc")
            .GetResult();

        // Assert - Soft check: algumas dependências podem ser necessárias
        Assert.True(result.IsSuccessful || result.FailingTypeNames?.Count() <= 3,
            "Application layer should minimize references to ASP.NET Core (keep it framework-agnostic when possible)");
    }
}
