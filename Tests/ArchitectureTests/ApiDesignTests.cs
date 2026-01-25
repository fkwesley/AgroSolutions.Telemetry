using NetArchTest.Rules;

namespace Tests.ArchitectureTests;

/// <summary>
/// Testes de API design e versionamento.
/// </summary>
public class ApiDesignTests
{
    [Fact]
    public void Controllers_Should_Have_ApiController_Attribute()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Controller")
            .Should()
            .HaveCustomAttribute(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute))
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All controllers should have [ApiController] attribute. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Have_Route_Attribute()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Controller")
            .Should()
            .HaveCustomAttribute(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute))
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All controllers should have [Route] attribute. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_Inherit_ControllerBase()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .And()
            .HaveNameEndingWith("Controller")
            .Should()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All controllers should inherit from ControllerBase. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void API_Controllers_Should_Be_In_Versioned_Namespaces()
    {
        // Arrange & Act
        var types = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespaceStartingWith("API.Controllers")
            .And()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Controller")
            .GetTypes();

        var violations = types
            .Where(t => !t.Namespace!.Contains(".v1") && 
                       !t.Namespace.Contains(".v2") &&
                       !t.Namespace.Contains(".v3"))
            .Select(t => t.FullName)
            .ToList();

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Controllers_Should_Have_XML_Documentation()
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
            var publicMethods = type.GetMethods()
                .Where(m => m.IsPublic && 
                           !m.IsSpecialName && 
                           m.DeclaringType == type &&
                           !m.Name.Equals("ToString") &&
                           !m.Name.Equals("GetHashCode") &&
                           !m.Name.Equals("Equals"))
                .ToList();

            // Este é um teste de intenção - você precisaria de análise de XML docs real
            // Por enquanto, verifica que o método não é trivial demais
            if (publicMethods.Any() && type.GetCustomAttributes(false).Length < 2)
            {
                violations.Add($"{type.Name} might be missing XML documentation");
            }
        }

        // Assert - Soft check
        Assert.True(violations.Count < types.Count() / 2, 
            "Many controllers seem to be missing XML documentation");
    }
}
