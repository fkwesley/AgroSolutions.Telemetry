using NetArchTest.Rules;
using Xunit;

namespace Tests.ArchitectureTests
{
    public class LayerDependencyTests
    {
        [Fact]
        public void Domain_ShouldNotHaveDependencyOn_Application()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .ShouldNot()
                .HaveDependencyOn("Application")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Domain layer should not depend on Application layer");
        }

        [Fact]
        public void Domain_ShouldNotHaveDependencyOn_Infrastructure()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .ShouldNot()
                .HaveDependencyOn("Infrastructure")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Domain layer should not depend on Infrastructure layer");
        }

        [Fact]
        public void Domain_ShouldNotHaveDependencyOn_API()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .ShouldNot()
                .HaveDependencyOn("API")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Domain layer should not depend on API layer");
        }

        [Fact]
        public void Application_ShouldNotHaveDependencyOn_Infrastructure()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Application.Interfaces.IFieldMeasurementService).Assembly)
                .ShouldNot()
                .HaveDependencyOn("Infrastructure")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Application layer should not depend on Infrastructure layer");
        }

        [Fact]
        public void Application_ShouldNotHaveDependencyOn_API()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Application.Interfaces.IFieldMeasurementService).Assembly)
                .ShouldNot()
                .HaveDependencyOn("API")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Application layer should not depend on API layer");
        }

        [Fact]
        public void Infrastructure_ShouldNotHaveDependencyOn_API()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Infrastructure.Repositories.FieldMeasurementRepository).Assembly)
                .ShouldNot()
                .HaveDependencyOn("API")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Infrastructure layer should not depend on API layer");
        }

        [Fact]
        public void API_ShouldNotHaveDependencyOn_Infrastructure_Except_DI()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .ShouldNot()
                .HaveDependencyOn("Infrastructure")
                .GetResult();

            // Assert - Controllers não devem depender diretamente de Infrastructure
            var result2 = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .DoNotResideInNamespace("API.Configurations")
                .ShouldNot()
                .HaveDependencyOn("Infrastructure")
                .GetResult();

            Assert.True(result.IsSuccessful, "API Controllers should not depend on Infrastructure directly");
        }
    }
}
