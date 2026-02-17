using NetArchTest.Rules;
using Xunit;

namespace Tests.ArchitectureTests
{
    public class NamingConventionTests
    {
        [Fact]
        public void Interfaces_ShouldStartWith_I()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Application.Interfaces.IFieldMeasurementService).Assembly)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "All interfaces should start with 'I'");
        }

        [Fact]
        public void Services_ShouldEndWith_Service()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Application.Interfaces.IFieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .And()
                .AreClasses()
                .And()
                .DoNotHaveName("DomainEventDispatcher") // Dispatcher não é um Service típico
                .Should()
                .HaveNameEndingWith("Service")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "All service classes should end with 'Service'");
        }

        [Fact]
        public void Controllers_ShouldEndWith_Controller()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .And()
                .AreClasses()
                .Should()
                .HaveNameEndingWith("Controller")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "All controller classes should end with 'Controller'");
        }

        [Fact]
        public void DTOs_Request_ShouldEndWith_Request()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.DTO.FieldMeasurement.AddFieldMeasurementRequest).Assembly)
                .That()
                .ResideInNamespace("Application.DTO")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert - DTOs devem ter nomenclatura consistente
            foreach (var type in types)
            {
                var typeName = type.Name.Contains('`') ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name;

                var hasValidSuffix = typeName.EndsWith("Request") ||
                                   typeName.EndsWith("Response") ||
                                   typeName.EndsWith("Parameters") ||
                                   typeName == "Link" ||
                                   typeName == "PagedResponse" ||
                                   typeName == "ComponentHealth" ||
                                   typeName == "HealthResponse";

                Assert.True(hasValidSuffix, 
                    $"{type.Name} should end with 'Request', 'Response', 'Parameters' or be a known DTO type");
            }
        }

        [Fact]
        public void DTOs_Response_ShouldEndWith_Response()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.DTO.FieldMeasurement.FieldMeasurementResponse).Assembly)
                .That()
                .ResideInNamespace("Application.DTO")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert - DTOs devem ter nomenclatura consistente
            foreach (var type in types)
            {
                var typeName = type.Name.Contains('`') ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name;

                var hasValidSuffix = typeName.EndsWith("Request") ||
                                   typeName.EndsWith("Response") ||
                                   typeName.EndsWith("Parameters") ||
                                   typeName == "Link" ||
                                   typeName == "PagedResponse" ||
                                   typeName == "ComponentHealth" ||
                                   typeName == "HealthResponse";

                Assert.True(hasValidSuffix,
                    $"{type.Name} should end with 'Request', 'Response', 'Parameters' or be a known DTO type");
            }
        }

        [Fact]
        public void Repositories_ShouldEndWith_Repository()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Infrastructure.Repositories.FieldMeasurementRepository).Assembly)
                .That()
                .ResideInNamespace("Infrastructure.Repositories")
                .And()
                .AreClasses()
                .Should()
                .HaveNameEndingWith("Repository")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "All repository classes should end with 'Repository'");
        }

        [Fact]
        public void Entities_ShouldResideIn_DomainEntities()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .That()
                .Inherit(typeof(Domain.Common.BaseEntity))
                .Should()
                .ResideInNamespace("Domain.Entities")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "All entities should reside in Domain.Entities namespace");
        }

        [Fact]
        public void Events_ShouldEndWith_Event()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .That()
                .ResideInNamespace("Domain.Events")
                .And()
                .AreClasses()
                .And()
                .DoNotHaveNameMatching("IDomainEvent")
                .Should()
                .HaveNameEndingWith("Event")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "All event classes should end with 'Event'");
        }
    }
}
