namespace API.Middlewares
{
    /// <summary>
    /// Middleware que adiciona headers HTTP de cache em todas as respostas.
    /// 
    /// 🎯 OBJETIVO:
    /// Informar explicitamente aos clientes e proxies para NÃO cachearem as respostas.
    /// Importante para APIs onde dados frescos são críticos (ex: pedidos, pagamentos).
    /// 
    /// 📋 HEADERS ADICIONADOS:
    /// - Cache-Control: no-cache, no-store, must-revalidate
    /// - Pragma: no-cache (compatibilidade HTTP/1.0)
    /// - Expires: 0 (garante que não cache)
    /// 
    /// ⚙️ IMPORTANTE:
    /// Headers são adicionados ANTES de processar a requisição para garantir
    /// que não conflitem com o envio da resposta.
    /// </summary>
    public class NoCacheMiddleware
    {
        private readonly RequestDelegate _next;

        public NoCacheMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 👈 Adiciona headers ANTES de processar o request
            // Isso garante que eles sejam incluídos na resposta
            context.Response.OnStarting(() =>
            {
                // Só adiciona headers no-cache se a resposta for bem-sucedida
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    if (!context.Response.Headers.ContainsKey("Cache-Control"))
                        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    
                    if (!context.Response.Headers.ContainsKey("Pragma"))
                        context.Response.Headers["Pragma"] = "no-cache";
                    
                    if (!context.Response.Headers.ContainsKey("Expires"))
                        context.Response.Headers["Expires"] = "0";
                }
                
                return Task.CompletedTask;
            });

            // Executa o resto do pipeline
            await _next(context);
        }
    }
}

