namespace Application.DTO.Common
{
    /// <summary>
    /// Representa um link HATEOAS (Hypermedia as the Engine of Application State).
    /// 
    /// ?? OBJETIVO:
    /// Permite que a API informe ao cliente quais ações estão disponíveis,
    /// eliminando a necessidade do cliente conhecer a estrutura de URLs.
    /// 
    /// ?? EXEMPLO:
    /// {
    ///   "href": "/v2/orders/123",
    ///   "rel": "self",
    ///   "method": "GET"
    /// }
    /// </summary>
    public class Link
    {
        /// <summary>
        /// URL completa ou relativa do recurso.
        /// Exemplo: "/v2/orders/123" ou "https://api.example.com/v2/orders/123"
        /// </summary>
        public string Href { get; set; } = string.Empty;

        /// <summary>
        /// Relação deste link com o recurso atual.
        /// 
        /// Valores comuns:
        /// - "self": O próprio recurso
        /// - "next": Próximo recurso em uma lista
        /// - "prev" ou "previous": Recurso anterior
        /// - "first": Primeiro recurso
        /// - "last": Último recurso
        /// - "update": URL para atualizar
        /// - "delete": URL para deletar
        /// - "items": Lista de itens relacionados
        /// </summary>
        public string Rel { get; set; } = string.Empty;

        /// <summary>
        /// Método HTTP a ser usado (GET, POST, PUT, PATCH, DELETE).
        /// Opcional: Se não informado, assume GET.
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// Construtor vazio para serialização.
        /// </summary>
        public Link() { }

        /// <summary>
        /// Construtor para criar um link facilmente.
        /// </summary>
        public Link(string href, string rel, string? method = null)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }
    }
}
