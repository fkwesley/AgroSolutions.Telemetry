using API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

namespace API.Configurations;

public static class AuthenticationConfiguration
{
    public static WebApplicationBuilder AddAuthenticationConfiguration(this WebApplicationBuilder builder)
    {
        var jwtKey = builder.Configuration["Jwt:Key"] 
            ?? throw new ArgumentNullException("Jwt:Key", "JWT Key not found in configuration.");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
            ?? throw new ArgumentNullException("Jwt:Issuer", "JWT Issuer not found in configuration.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var result = new ErrorResponse
                        {
                            Message = "Invalid Token, not Authenticated."
                        };

                        var json = JsonSerializer.Serialize(result);
                        return context.Response.WriteAsync(json);
                    },

                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";

                        var result = new ErrorResponse
                        {
                            Message = "Access Denied! You do not have permission to perform this operation."
                        };

                        var json = JsonSerializer.Serialize(result);
                        return context.Response.WriteAsync(json);
                    }
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
