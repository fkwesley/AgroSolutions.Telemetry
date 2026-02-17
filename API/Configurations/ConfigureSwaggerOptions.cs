using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Configurations
{
    /// <summary>
    /// DYNAMIC Swagger configuration for multi-version API support.
    /// 
    /// 🎯 OBJECTIVE:
    /// - AUTOMATICALLY detects all API versions (v1, v2, v3...) based on controllers
    /// - Creates a separate Swagger document for each discovered version
    /// - Automatically marks deprecated versions in documentation
    /// - Adds version-specific metadata
    /// 
    /// 📘 WHY IT'S NECESSARY:
    /// Without this class, you would have to MANUALLY configure each version in Program.cs
    /// 
    /// 🔧 HOW IT WORKS:
    /// 1. IApiVersionDescriptionProvider discovers all versions through [ApiVersion] attributes
    /// 2. For each version found, creates a Swagger document with specific metadata
    /// 3. Checks if version is marked as Deprecated and adds warning
    /// </summary>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Configures Swagger for each automatically discovered API version.
        /// </summary>
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    CreateInfoForApiVersion(description));
            }
        }

        /// <summary>
        /// Creates metadata information for a specific API version.
        /// </summary>
        private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo
            {
                Title = "AgroSolutions.Telemetry.API",
                Version = description.ApiVersion.ToString(),
                Description = "API for receiving and processing telemetry data from agricultural field thermal sensors.",
                Contact = new OpenApiContact
                {
                    Name = "AgroSolutions Team",
                    Email = "support@agrosolutions.com"
                }
            };

            if (description.IsDeprecated)
            {
                info.Description += " - ⚠️ This API version has been deprecated.";
            }

            return info;
        }
    }
}
