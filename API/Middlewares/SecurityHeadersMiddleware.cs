namespace API.Middlewares
{
    /// <summary>
    /// Middleware que adiciona headers de segurança HTTP em todas as respostas.
    /// 
    /// ?? OBJETIVO:
    /// Proteger a API contra ataques comuns (XSS, Clickjacking, MITM, etc)
    /// seguindo as recomendações do OWASP Secure Headers Project.
    /// 
    /// ?? HEADERS ADICIONADOS:
    /// 
    /// 1. X-Content-Type-Options: nosniff
    ///    - Previne MIME Type Sniffing Attack
    ///    - Força o browser a respeitar o Content-Type enviado
    /// 
    /// 2. X-Frame-Options: DENY
    ///    - Previne Clickjacking Attack
    ///    - Impede que a API seja carregada em iframes
    /// 
    /// 3. X-XSS-Protection: 1; mode=block
    ///    - Proteção XSS para navegadores legacy
    ///    - Browser bloqueia a página se detectar XSS
    /// 
    /// 4. Strict-Transport-Security (HSTS)
    ///    - Previne Man-in-the-Middle e SSL Stripping
    ///    - Força uso de HTTPS por 1 ano
    /// 
    /// 5. Referrer-Policy: strict-origin-when-cross-origin
    ///    - Previne vazamento de informações sensíveis
    ///    - Envia apenas origem em requests cross-origin
    /// 
    /// 6. X-Permitted-Cross-Domain-Policies: none
    ///    - Previne Flash/PDF fazendo requests cross-domain
    /// 
    /// ?? IMPORTANTE:
    /// Este middleware deve ser registrado ANTES dos endpoints (MapControllers)
    /// para garantir que os headers sejam adicionados em todas as respostas.
    /// 
    /// ?? REFERÊNCIAS:
    /// - OWASP Secure Headers Project: https://owasp.org/www-project-secure-headers/
    /// - Mozilla Observatory: https://observatory.mozilla.org/
    /// - Security Headers: https://securityheaders.com/
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Registra callback para adicionar headers antes do envio da resposta
            context.Response.OnStarting(() =>
            {
                AddSecurityHeaders(context);
                return Task.CompletedTask;
            });

            await _next(context);
        }

        private static void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // 1. X-Content-Type-Options: nosniff
            // Previne MIME Type Sniffing Attack
            // Browser DEVE respeitar o Content-Type enviado pelo servidor
            if (!headers.ContainsKey("X-Content-Type-Options"))
            {
                headers["X-Content-Type-Options"] = "nosniff";
            }

            // 2. X-Frame-Options: DENY
            // Previne Clickjacking Attack
            // API não pode ser carregada em iframe/frame/embed/object
            if (!headers.ContainsKey("X-Frame-Options"))
            {
                headers["X-Frame-Options"] = "DENY";
            }

            // 3. X-XSS-Protection: 1; mode=block (Legacy)
            // Proteção XSS para navegadores antigos (IE, Edge Legacy)
            // Navegadores modernos têm proteção nativa via CSP
            if (!headers.ContainsKey("X-XSS-Protection"))
            {
                headers["X-XSS-Protection"] = "1; mode=block";
            }

            // 4. Strict-Transport-Security (HSTS)
            // Previne Man-in-the-Middle Attack e SSL Stripping
            // Apenas adiciona se a conexão for HTTPS
            if (context.Request.IsHttps && !headers.ContainsKey("Strict-Transport-Security"))
            {
                headers["Strict-Transport-Security"] = 
                    "max-age=31536000; includeSubDomains; preload";
                // max-age=31536000: 1 ano (em segundos)
                // includeSubDomains: Aplica a todos os subdomínios
                // preload: Permite inclusão na lista HSTS preload dos browsers
            }

            // 5. Referrer-Policy
            // Controla quanta informação é enviada no header Referer
            // strict-origin-when-cross-origin: Full URL para mesma origem, apenas origem para cross-origin
            if (!headers.ContainsKey("Referrer-Policy"))
            {
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            }

            // 6. X-Permitted-Cross-Domain-Policies
            // Previne Flash, PDF, etc de fazer requests cross-domain
            // "none": Nenhuma política cross-domain permitida
            if (!headers.ContainsKey("X-Permitted-Cross-Domain-Policies"))
            {
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
            }

            // OPCIONAL: Cross-Origin-Embedder-Policy (COEP)
            // Requer que todos os recursos sejam carregados com CORS ou mesma origem
            // Descomente se necessário (pode quebrar recursos externos)
            // if (!headers.ContainsKey("Cross-Origin-Embedder-Policy"))
            // {
            //     headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            // }

            // OPCIONAL: Cross-Origin-Opener-Policy (COOP)
            // Isola o contexto de navegação de outros contextos
            // Descomente se necessário
            // if (!headers.ContainsKey("Cross-Origin-Opener-Policy"))
            // {
            //     headers["Cross-Origin-Opener-Policy"] = "same-origin";
            // }

            // OPCIONAL: Cross-Origin-Resource-Policy (CORP)
            // Controla quem pode carregar este recurso
            // Descomente se necessário
            // if (!headers.ContainsKey("Cross-Origin-Resource-Policy"))
            // {
            //     headers["Cross-Origin-Resource-Policy"] = "same-origin";
            // }
        }
    }
}
