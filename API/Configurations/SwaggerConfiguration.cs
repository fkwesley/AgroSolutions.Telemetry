using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace API.Configurations;

public static class SwaggerConfiguration
{
    public static WebApplicationBuilder AddSwaggerConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT no formato: Bearer {seu_token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            c.OperationFilter<RemoveIgnoredPropertiesFilter>();
        });

        builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

        return builder;
    }

    public static WebApplication UseSwaggerConfiguration(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            var descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions
                .OrderByDescending(d => d.ApiVersion);

            foreach (var description in descriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"FCG.Orders.API {description.GroupName.ToUpperInvariant()}"
                );
            }

            options.DefaultModelsExpandDepth(1);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.DisplayRequestDuration();
        });

        return app;
    }
}
