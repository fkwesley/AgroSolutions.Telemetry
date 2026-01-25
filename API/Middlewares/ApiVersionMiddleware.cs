using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace API.Middlewares
{
    /// <summary>
    /// Middleware para garantir que os headers de versionamento sejam adicionados em TODAS as respostas.
    /// 
    /// ?? OBJETIVO:
    /// - Adiciona headers: api-supported-versions e api-deprecated-versions
    /// - Garante que os headers apareçam mesmo quando há erros ou middlewares customizados
    /// 
    /// ?? HEADERS ADICIONADOS:
    /// - api-supported-versions: Lista todas as versões disponíveis (ex: 1.0, 2.0-preview)
    /// - api-deprecated-versions: Lista versões marcadas como deprecated
    /// 
    /// ?? IMPORTANTE:
    /// Este middleware DEVE ser registrado ANTES de app.MapControllers() no Program.cs
    /// </summary>
    public class ApiVersionMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiVersionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IApiVersionDescriptionProvider provider)
        {
            // Coleta todas as versões suportadas
            var supportedVersions = provider.ApiVersionDescriptions
                .Select(d => d.ApiVersion.ToString())
                .Distinct()
                .OrderBy(v => v);

            // Coleta versões deprecated
            var deprecatedVersions = provider.ApiVersionDescriptions
                .Where(d => d.IsDeprecated)
                .Select(d => d.ApiVersion.ToString())
                .Distinct()
                .OrderBy(v => v);

            // Adiciona header com versões suportadas
            context.Response.Headers["api-supported-versions"] = string.Join(", ", supportedVersions);

            // Adiciona header com versões deprecated (se houver)
            if (deprecatedVersions.Any())
            {
                context.Response.Headers["api-deprecated-versions"] = string.Join(", ", deprecatedVersions);
            }

            // Executa o próximo middleware
            await _next(context);
        }
    }
}
