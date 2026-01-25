using NetArchTest.Rules;

namespace Tests.ArchitectureTests;

/// <summary>
/// Testes que garantem naming conventions e organização do código.
/// </summary>
public class NamingConventionTests
{
    [Fact]
    public void Services_Should_HaveName_EndingWith_Service()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.Interfaces.IOrderService).Assembly)
            .That()
            .ResideInNamespace("Application.Services")
            .And()
            .AreClasses()
            .And()
            .DoNotHaveName("DomainEventDispatcher") // Dispatcher é um padrão válido
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All services should end with 'Service'. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Interfaces_Should_StartWith_I()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.Interfaces.IOrderService).Assembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All interfaces should start with 'I'. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_Should_HaveName_EndingWith_Controller()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(API.Controllers.v2.OrdersController).Assembly)
            .That()
            .ResideInNamespace("API.Controllers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All controllers should end with 'Controller'. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DTOs_WithRequest_Should_HaveName_EndingWith_Request()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.DTO.Order.AddOrderRequest).Assembly)
            .That()
            .ResideInNamespaceMatching("Application.DTO.*")
            .And()
            .AreClasses()
            .And()
            .HaveNameMatching(".*Request$")
            .Should()
            .HaveNameEndingWith("Request")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            "Request DTOs should end with 'Request'");
    }

    [Fact]
    public void DTOs_WithResponse_Should_HaveName_EndingWith_Response()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Application.DTO.Order.OrderResponse).Assembly)
            .That()
            .ResideInNamespaceMatching("Application.DTO.*")
            .And()
            .AreClasses()
            .And()
            .HaveNameMatching(".*Response$")
            .Should()
            .HaveNameEndingWith("Response")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            "Response DTOs should end with 'Response'");
    }

    [Fact]
    public void Repositories_Should_HaveName_EndingWith_Repository()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Infrastructure.Context.OrdersDbContext).Assembly)
            .That()
            .ResideInNamespaceMatching("Infrastructure.Repositories.*")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All repositories should end with 'Repository'. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Entities_Should_ResideIn_DomainEntities_Namespace()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .That()
            .Inherit(typeof(Domain.Common.BaseEntity))
            .Should()
            .ResideInNamespace("Domain.Entities")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All entities should reside in Domain.Entities namespace. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Enums_Should_ResideIn_DomainEnums_Namespace()
    {
        // Arrange
        var types = Types.InAssembly(typeof(Domain.Enums.OrderStatus).Assembly)
            .That()
            .ResideInNamespace("Domain.Enums")
            .GetTypes()
            .Where(t => t.IsEnum)
            .ToList();

        // Assert
        Assert.NotEmpty(types);
        Assert.All(types, t => Assert.Equal("Domain.Enums", t.Namespace));
    }

    [Fact]
    public void Exceptions_Should_HaveName_EndingWith_Exception()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Domain.Entities.Order).Assembly)
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .HaveNameEndingWith("Exception")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All custom exceptions should end with 'Exception'. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
