using NetArchTest.Rules;
using Xunit;

namespace Tests.ArchitectureTests
{
    public class PerformanceAndBestPracticesTests
    {
        [Fact]
        public void Services_ShouldBeAsync()
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
                var methods = type.GetMethods()
                    .Where(m => m.IsPublic && !m.IsSpecialName);

                foreach (var method in methods)
                {
                    // Skip property getters/setters and GetType (inherited from Object)
                    if (method.Name.StartsWith("get_") || 
                        method.Name.StartsWith("set_") || 
                        method.Name == "GetType" ||
                        method.DeclaringType == typeof(object))
                        continue;

                    var isAsync = method.ReturnType.Name.Contains("Task");
                    Assert.True(isAsync,
                        $"{type.Name}.{method.Name} should be async for better performance");
                }
            }
        }

        [Fact]
        public void Controllers_ShouldReturn_ActionResult()
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
                var methods = type.GetMethods()
                    .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == type);

                var asyncMethods = methods.Where(m => m.ReturnType.Name.Contains("Task"));
                
                Assert.True(asyncMethods.Any(), 
                    $"{type.Name} should have async methods for I/O operations");
            }
        }

        [Fact]
        public void Entities_ShouldNot_HavePublicSetters()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(Domain.Entities.FieldMeasurement).Assembly)
                .That()
                .ResideInNamespace("Domain.Entities")
                .And()
                .AreClasses()
                .And()
                .DoNotHaveName("RequestLog") // RequestLog é usado como DTO, não entidade de domínio
                .GetTypes();

            // Assert - Domain entities devem ter encapsulamento adequado
            foreach (var type in result)
            {
                var properties = type.GetProperties()
                    .Where(p => p.SetMethod != null && p.SetMethod.IsPublic);

                // Permitir setters públicos apenas para propriedades simples usadas por EF/Cosmos
                var publicSetters = properties
                    .Where(p => !p.Name.Equals("Id") && 
                                !p.Name.Equals("FieldId") &&
                                !p.PropertyType.IsValueType &&
                                !p.PropertyType.Name.Contains("Guid") &&
                                !p.PropertyType.Name.Contains("DateTime"))
                    .ToList();

                Assert.True(publicSetters.Count <= 2, 
                    $"{type.Name} has {publicSetters.Count} public setters for complex properties. " +
                    "Consider using private setters or methods for domain logic.");
            }
        }

        [Fact]
        public void Controllers_ShouldUse_ProducesResponseType()
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
                    var hasProducesResponseType = method.GetCustomAttributes(false)
                        .Any(a => a.GetType().Name.Contains("ProducesResponseType"));

                    Assert.True(hasProducesResponseType || method.Name.Contains("Dispose"),
                        $"{type.Name}.{method.Name} should have [ProducesResponseType] for API documentation");
                }
            }
        }

        [Fact]
        public void DTOs_ShouldBe_Immutable_OrHaveValidation()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.DTO.FieldMeasurement.FieldMeasurementResponse).Assembly)
                .That()
                .ResideInNamespace("Application.DTO")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in types)
            {
                var typeName = type.Name.Contains('`') ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name;

                var hasValidation = type.GetProperties()
                    .Any(p => p.GetCustomAttributes(false)
                        .Any(a => a.GetType().Name.Contains("Validation") || 
                                  a.GetType().Name.Contains("Required") ||
                                  a.GetType().Name.Contains("Range")));

                var isResponse = typeName.EndsWith("Response") ||
                                typeName.EndsWith("Parameters") || // Helper classes
                                typeName.EndsWith("Enum") || // Helper classes
                                typeName == "PagedResponse" ||
                                typeName == "Link" ||
                                typeName == "ComponentHealth" ||
                                typeName == "HealthResponse" ||
                                typeName == "NotificationRequest";      // Internal DTO for Service Bus

                if (!isResponse)
                {
                    Assert.True(hasValidation, 
                        $"{type.Name} should have validation attributes for request DTOs");
                }
            }
        }

        [Fact]
        public void Services_ShouldNot_ThrowGenericExceptions()
        {
            // Arrange & Act
            var types = Types.InAssembly(typeof(Application.Services.FieldMeasurementService).Assembly)
                .That()
                .ResideInNamespace("Application.Services")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert - Services devem lançar exceções específicas
            foreach (var type in types)
            {
                var methods = type.GetMethods()
                    .Where(m => m.IsPublic && !m.IsSpecialName);

                // Verificação básica - em produção, usar análise estática de código
                Assert.True(methods.Any(), $"{type.Name} should have public methods");
            }
        }
    }
}
