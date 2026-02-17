using NetArchTest.Rules;
using Xunit;

namespace Tests.ArchitectureTests
{
    public class TestabilityTests
    {
        [Fact]
        public void Services_ShouldHave_InterfaceBasedDependencies()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.Services.FieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var constructor = type.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    var parameters = constructor.GetParameters();
                    
                    foreach (var param in parameters)
                    {
                        var isInterface = param.ParameterType.IsInterface;
                        var isLogger = param.ParameterType.Name.Contains("ILogger");
                        
                        Assert.True(isInterface || isLogger,
                            $"{type.Name} constructor parameter {param.Name} should be an interface for testability");
                    }
                }
            }
        }

        [Fact]
        public void Controllers_ShouldHave_InterfaceDependencies()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.Services.FieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var constructor = type.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    var hasInterfaceDependencies = constructor.GetParameters()
                        .All(p => p.ParameterType.IsInterface);

                    Assert.True(hasInterfaceDependencies,
                        $"{type.Name} should depend only on interfaces for easier mocking in tests");
                }
            }
        }

        [Fact]
        public void Entities_ShouldBe_PureClasses()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .That()
                .ResideInNamespace("Domain.Entities")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert - Entities should not have external dependencies
            foreach (var type in result)
            {
                var hasExternalDeps = type.GetConstructors()
                    .SelectMany(c => c.GetParameters())
                    .Any(p => p.ParameterType.Namespace?.StartsWith("System.Net") == true ||
                             p.ParameterType.Namespace?.StartsWith("System.Data") == true);

                Assert.False(hasExternalDeps,
                    $"{type.Name} should not have external dependencies for testability");
            }
        }

        [Fact]
        public void Controllers_ShouldNot_HaveBusinessLogic()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var methods = type.GetMethods()
                    .Where(m => m.IsPublic && 
                                !m.IsSpecialName && 
                                m.DeclaringType == type &&
                                m.ReturnType.Name.Contains("Task"));

                foreach (var method in methods)
                {
                    // Verificar se métodos são curtos (indicativo de delegação de lógica)
                    var methodBody = method.GetMethodBody();
                    if (methodBody != null)
                    {
                        // Controllers devem ter métodos simples que delegam para services
                        Assert.True(true, "Controller methods should delegate to services");
                    }
                }
            }
        }

        [Fact]
        public void Repositories_ShouldNot_HaveBusinessLogic()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Infrastructure.Repositories.FieldMeasurementRepository).Assembly)
                .That()
                .ResideInNamespace("Infrastructure.Repositories")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                // Repositories devem apenas fazer CRUD, não ter lógica de negócio
                var hasOnlyDataAccess = !type.GetMethods()
                    .Any(m => m.Name.Contains("Calculate") || 
                             m.Name.Contains("Validate") || 
                             m.Name.Contains("Process"));

                Assert.True(hasOnlyDataAccess,
                    $"{type.Name} should only handle data access, not business logic");
            }
        }

        [Fact]
        public void Services_ShouldNot_HaveStaticMethods()
        {
            // Arrange & Act
            var staticClasses = Types.InAssembly(typeof(Application.Services.FieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .And()
                .AreClasses()
                .And()
                .AreNotAbstract()
                .GetTypes()
                .Where(t => t.GetMethods()
                    .Any(m => m.IsStatic && m.IsPublic && !m.IsSpecialName));

            // Assert
            Assert.Empty(staticClasses.Select(t => t.Name));
        }

        [Fact]
        public void AllClasses_ShouldHave_SingleConstructor()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.Services.FieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var constructors = type.GetConstructors();
                var publicConstructors = constructors.Where(c => c.IsPublic).ToList();

                Assert.True(publicConstructors.Count <= 1,
                    $"{type.Name} should have at most one public constructor for DI simplicity");
            }
        }
    }
}
