using NetArchTest.Rules;
using Xunit;

namespace Tests.ArchitectureTests
{
    public class SolidPrinciplesTests
    {
        // Single Responsibility Principle (SRP)
        [Fact]
        public void Services_ShouldHave_SingleResponsibility()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.Interfaces.IFieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert - Cada serviço deve ter um único propósito
            // Verificar se classes de serviço não têm muitas dependências (indicativo de múltiplas responsabilidades)
            foreach (var type in types)
            {
                var constructors = type.GetConstructors();
                if (constructors.Length > 0)
                {
                    var parameters = constructors[0].GetParameters();
                    Assert.True(parameters.Length <= 6, 
                        $"{type.Name} has {parameters.Length} dependencies. Consider refactoring (SRP violation).");
                }
            }
        }

        // Open/Closed Principle (OCP)
        [Fact]
        public void Repositories_ShouldImplement_Interfaces()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Application.Services.FieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .Should()
                .HaveDependencyOn("Application.Interfaces")
                .GetResult();

            // Assert - Services devem depender de abstrações (interfaces)
            Assert.True(result.IsSuccessful, "Services should depend on interfaces (OCP)");
        }

        // Liskov Substitution Principle (LSP)
        [Fact]
        public void Controllers_ShouldDependOn_InterfacesNotImplementations()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .ShouldNot()
                .HaveDependencyOn("Application.Services")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, 
                "Controllers should depend on interfaces, not concrete implementations (LSP)");
        }

        // Interface Segregation Principle (ISP)
        [Fact]
        public void Interfaces_ShouldBe_Focused()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.Interfaces.IFieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Interfaces")
                .And()
                .AreInterfaces()
                .GetTypes();

            // Assert - Interfaces não devem ter muitos métodos
            foreach (var type in types)
            {
                var methods = type.GetMethods();
                Assert.True(methods.Length <= 10, 
                    $"{type.Name} has {methods.Length} methods. Consider splitting (ISP violation).");
            }
        }

        // Dependency Inversion Principle (DIP)
        [Fact]
        public void Domain_ShouldNot_DependOnConcreteTypes()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .That()
                .ResideInNamespace("Domain")
                .ShouldNot()
                .HaveDependencyOn("Infrastructure")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Domain should not depend on concrete Infrastructure types (DIP)");
        }
    }
}
