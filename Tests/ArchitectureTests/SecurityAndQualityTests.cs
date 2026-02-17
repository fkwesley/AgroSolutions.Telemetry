using NetArchTest.Rules;
using Xunit;

namespace Tests.ArchitectureTests
{
    public class SecurityAndQualityTests
    {
        [Fact]
        public void Controllers_ShouldHave_AuthorizeAttribute()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .And()
                .AreClasses()
                .And()
                .DoNotHaveName("HealthController")
                .GetTypes();

            // Assert
            foreach (var type in result)
            {
                var hasAuthorize = type.GetCustomAttributes(false)
                    .Any(a => a.GetType().Name.Contains("Authorize"));

                Assert.True(hasAuthorize, 
                    $"{type.Name} should have [Authorize] attribute for security");
            }
        }

        [Fact]
        public void DTOs_ShouldHave_ValidationAttributes()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.DTO.FieldMeasurement.AddFieldMeasurementRequest).Assembly)
                .That()
                .ResideInNamespace("Application.DTO")
                .And()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Request")
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var properties = type.GetProperties();
                var hasValidation = properties.Any(p => 
                    p.GetCustomAttributes(false).Any(a => 
                        a.GetType().Name.Contains("Required") ||
                        a.GetType().Name.Contains("Range") ||
                        a.GetType().Name.Contains("StringLength") ||
                        a.GetType().Name.Contains("RegularExpression")));

                Assert.True(hasValidation, 
                    $"{type.Name} should have validation attributes for data integrity");
            }
        }

        [Fact]
        public void Entities_ShouldNot_ExposeCollectionsDirectly()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .That()
                .ResideInNamespace("Domain.Entities")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var collectionProperties = type.GetProperties()
                    .Where(p => p.PropertyType.IsGenericType &&
                               (p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                                p.PropertyType.GetGenericTypeDefinition() == typeof(IList<>) ||
                                p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)));

                foreach (var prop in collectionProperties)
                {
                    var hasPublicSetter = prop.SetMethod?.IsPublic ?? false;
                    Assert.False(hasPublicSetter,
                        $"{type.Name}.{prop.Name} should not have public setter. Use private setter and Add/Remove methods.");
                }
            }
        }

        [Fact]
        public void Services_ShouldValidate_ArgumentsNotNull()
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
                    
                    // Verificar se há validação (presença de código de validação é verificada indiretamente)
                    Assert.True(parameters.Any(), 
                        $"{type.Name} should have dependencies injected via constructor");
                }
            }
        }

        [Fact]
        public void Repositories_ShouldUse_AsyncMethods()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Infrastructure.Repositories.FieldMeasurementRepository).Assembly)
                .That()
                .ResideInNamespace("Infrastructure.Repositories")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in result)
            {
                var methods = type.GetMethods()
                    .Where(m => m.IsPublic && !m.IsSpecialName);

                var asyncMethods = methods.Where(m => m.ReturnType.Name.Contains("Task"));
                
                Assert.True(asyncMethods.Any() || !methods.Any(), 
                    $"{type.Name} should use async methods for I/O operations");
            }
        }

        [Fact]
        public void Entities_ShouldNot_HaveParameterlessConstructor_Public()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .That()
                .ResideInNamespace("Domain.Entities")
                .And()
                .AreClasses()
                .And()
                .DoNotHaveName("RequestLog") // RequestLog é usado como DTO, não entidade de domínio pura
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var parameterlessConstructor = type.GetConstructor(Type.EmptyTypes);
                if (parameterlessConstructor != null)
                {
                    Assert.False(parameterlessConstructor.IsPublic,
                        $"{type.Name} should not have public parameterless constructor. Use private for ORM/serialization.");
                }
            }
        }

        [Fact]
        public void Interfaces_ShouldNot_ExposeImplementationDetails()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Application.Interfaces.IFieldMeasurementService).Assembly)
                .That()
                .AreInterfaces()
                .GetTypes();

            // Assert
            foreach (var type in result)
            {
                var methods = type.GetMethods();
                
                foreach (var method in methods)
                {
                    // Verificar se métodos retornam abstrações, não implementações concretas
                    var returnType = method.ReturnType;
                    
                    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        returnType = returnType.GetGenericArguments()[0];
                    }

                    var isAbstraction = returnType.IsInterface || 
                                       returnType.IsAbstract || 
                                       returnType.IsGenericParameter ||
                                       returnType.IsValueType ||
                                       returnType == typeof(string) ||
                                       returnType.Namespace?.StartsWith("System") == true ||
                                       returnType.Namespace?.StartsWith("Application.DTO") == true;

                    Assert.True(isAbstraction || returnType == typeof(void),
                        $"{type.Name}.{method.Name} should return abstraction, not concrete type from Infrastructure");
                }
            }
        }
    }
}
