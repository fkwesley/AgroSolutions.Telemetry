using API.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Configurations;

public static class ValidationConfiguration
{
    public static WebApplicationBuilder AddValidationConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x =>
                    {
                        var field = x.Key;
                        var messages = x.Value!.Errors.Select(e => e.ErrorMessage);
                        return $"{field}: {string.Join(" | ", messages)}";
                    });

                var response = new ErrorResponse
                {
                    Message = "One or more validation errors occurred.",
                    Detail = string.Join(" || ", errors),
                    LogId = null
                };

                return new BadRequestObjectResult(response);
            };
        });

        return builder;
    }
}
