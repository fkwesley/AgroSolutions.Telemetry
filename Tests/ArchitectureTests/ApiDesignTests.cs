using NetArchTest.Rules;
using Xunit;

namespace Tests.ArchitectureTests
{
    public class ApiDesignTests
    {
        [Fact]
        public void Controllers_ShouldHave_RouteAttribute()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in result)
            {
                var hasRouteAttribute = type.GetCustomAttributes(false)
                    .Any(a => a.GetType().Name.Contains("Route"));

                Assert.True(hasRouteAttribute, 
                    $"{type.Name} should have [Route] attribute for RESTful routing");
            }
        }

        [Fact]
        public void Controllers_ShouldHave_ApiControllerAttribute()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in result)
            {
                var hasApiController = type.GetCustomAttributes(false)
                    .Any(a => a.GetType().Name.Contains("ApiController"));

                Assert.True(hasApiController, 
                    $"{type.Name} should have [ApiController] attribute for automatic model validation");
            }
        }

        [Fact]
        public void ControllerActions_ShouldHave_HttpMethodAttribute()
        {
            // Arrange & Act
            var result = Types.InAssembly(typeof(API.Controllers.v1.FieldMeasurementsController).Assembly)
                .That()
                .ResideInNamespace("API.Controllers")
                .And()
                .AreClasses()
                .GetTypes();

            // Assert
            foreach (var type in result)
            {
                var methods = type.GetMethods()
                    .Where(m => m.IsPublic && 
                                !m.IsSpecialName && 
                                m.DeclaringType == type &&
                                m.ReturnType.Name.Contains("Task"));

                foreach (var method in methods)
                {
                    var hasHttpMethod = method.GetCustomAttributes(false)
                        .Any(a => a.GetType().Name.Contains("Http"));

                    Assert.True(hasHttpMethod || method.Name.Contains("Dispose"),
                        $"{type.Name}.{method.Name} should have HTTP method attribute ([HttpGet], [HttpPost], etc.)");
                }
            }
        }

        [Fact]
        public void Controllers_ShouldFollow_RESTNamingConventions()
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
                    .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == type);

                var methodNames = methods.Select(m => m.Name).ToList();

                // Verificar convenções REST comuns
                var hasRestVerbs = methodNames.Any(n => 
                    n.StartsWith("Get") || 
                    n.StartsWith("Post") || 
                    n.StartsWith("Put") || 
                    n.StartsWith("Delete") || 
                    n.StartsWith("Patch") ||
                    n.Contains("Add") ||
                    n.Contains("Update") ||
                    n.Contains("Remove"));

                Assert.True(hasRestVerbs || !methodNames.Any(), 
                    $"{type.Name} should follow REST naming conventions (Get, Post, Put, Delete, Patch)");
            }
        }

        [Fact]
        public void Controllers_ShouldReturn_ProperStatusCodes()
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
                                m.DeclaringType == type);

                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(false);
                    var httpMethod = attributes.FirstOrDefault(a => a.GetType().Name.StartsWith("Http"));

                    if (httpMethod != null)
                    {
                        var hasProducesResponseType = attributes
                            .Any(a => a.GetType().Name.Contains("ProducesResponseType"));

                        Assert.True(hasProducesResponseType || method.Name.Contains("Dispose"),
                            $"{type.Name}.{method.Name} should document expected HTTP status codes with [ProducesResponseType]");
                    }
                }
            }
        }
    }
}
