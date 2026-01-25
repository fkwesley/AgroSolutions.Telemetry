namespace API.Configurations
{
    /// <summary>
    /// Configuração CORS (Cross-Origin Resource Sharing) permissiva.
    /// 
    /// ?? OBJETIVO:
    /// Permitir que qualquer origem acesse a API sem bloqueios,
    /// adequado para APIs públicas ou com proteção em API Gateway.
    /// 
    /// ?? COMPORTAMENTO:
    /// - Desenvolvimento: Permite qualquer origem (localhost, etc)
    /// - Produção: Permite origens configuradas OU qualquer origem se não configurado
    /// 
    /// ?? SEGURANÇA:
    /// Rate limiting e autenticação ficam no API Gateway,
    /// então CORS aqui é permissivo para não causar problemas.
    /// </summary>
    public static class CorsConfiguration
    {
        private const string DefaultPolicyName = "DefaultCorsPolicy";

        public static IServiceCollection AddCorsConfiguration(
            this WebApplicationBuilder builder)
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(DefaultPolicyName, policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        // Desenvolvimento: Máxima permissividade
                        policy.SetIsOriginAllowed(_ => true)  // Permite qualquer origem
                              .AllowAnyMethod()                // GET, POST, PUT, PATCH, DELETE
                              .AllowAnyHeader()                // Qualquer header
                              .AllowCredentials();             // Permite cookies/auth
                    }
                    else if (allowedOrigins?.Length > 0)
                    {
                        // Produção com origens configuradas: Usa lista específica
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    }
                    else
                    {
                        // Produção sem configuração: Permissivo (não bloqueia)
                        policy.AllowAnyOrigin()       // Permite qualquer origem
                              .AllowAnyMethod()       // Permite qualquer método
                              .AllowAnyHeader();      // Permite qualquer header
                        // Nota: AllowAnyOrigin não pode usar AllowCredentials
                    }
                });
            });

            return builder.Services;
        }

        public static WebApplication UseCorsConfiguration(this WebApplication app)
        {
            // CORS deve vir ANTES de Authentication/Authorization
            app.UseCors(DefaultPolicyName);
            
            return app;
        }
    }
}
