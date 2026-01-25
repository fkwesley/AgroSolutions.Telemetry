using NetArchTest.Rules;

namespace Tests.ArchitectureTests;

/// <summary>
/// Testes que garantem que as dependências entre camadas seguem a Clean Architecture.
/// Regra geral: Domain não depende de nada, Application depende só de Domain,
/// Infrastructure depende de Domain e Application, API depende de todos.
/// </summary>
public class LayerDependencyTests
{
    private const string DomainNamespace = "Domain";
    private const string ApplicationNamespace = "Application";
    private const string InfrastructureNamespace = "Infrastructure";
    private const string ApiNamespace = "API";

    [Fact]
    public void Domain_Should_NotHaveDependencyOnOtherLayers()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            $"Domain layer should not depend on other layers. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Should_NotReferenceSystemDataSqlClient()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .ShouldNot()
            .HaveDependencyOn("System.Data.SqlClient")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            "Domain should not reference data access libraries like System.Data.SqlClient");
    }

    [Fact]
    public void Domain_Should_NotReferenceEntityFramework()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, 
            "Domain should not reference Entity Framework");
    }

    [Fact]
    public void Application_Should_NotHaveDependencyOn_Infrastructure_Or_API()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.Interfaces.IOrderService).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application layer should not depend on Infrastructure or API. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_OnlyDependOn_Domain()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.Interfaces.IOrderService).Assembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            "Application should only depend on Domain layer");
    }

    [Fact]
    public void Infrastructure_Should_NotHaveDependencyOn_API()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Infrastructure.Context.OrdersDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Infrastructure should not depend on API layer. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_NotDirectlyReference_Infrastructure()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .Should()
            .NotHaveDependencyOn("Infrastructure.Repositories")
            .GetResult();

        var result2 = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .Should()
            .NotHaveDependencyOn("Infrastructure.Context")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful && result2.IsSuccessful,
            "Controllers should use Application services, not Infrastructure directly");
    }
}
