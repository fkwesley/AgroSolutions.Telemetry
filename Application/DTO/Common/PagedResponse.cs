using System.Text.Json.Serialization;

namespace Application.DTO.Common
{
    /// <summary>
    /// Resposta paginada genérica com suporte a HATEOAS.
    /// </summary>
    /// <typeparam name="T">Tipo dos dados retornados (ex: OrderResponse)</typeparam>
    public class PagedResponse<T> : IHateoasResource
    {
        /// <summary>
        /// Lista de itens da página atual.
        /// </summary>
        public IEnumerable<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// Número da página atual (começa em 1).
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Quantidade de itens por página.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total de itens em todas as páginas.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total de páginas disponíveis.
        /// Cálculo: Ceiling(TotalCount / PageSize)
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Indica se existe página anterior.
        /// </summary>
        public bool HasPrevious => CurrentPage > 1;

        /// <summary>
        /// Indica se existe próxima página.
        /// </summary>
        public bool HasNext => CurrentPage < TotalPages;

        /// <summary>
        /// Links HATEOAS para navegação (self, next, prev, first, last).
        /// Aparece como "_links" no JSON.
        /// </summary>
        [JsonPropertyName("_links")]
        public List<Link> Links { get; set; } = new();

        /// <summary>
        /// Construtor vazio para serialização.
        /// </summary>
        public PagedResponse() { }

        /// <summary>
        /// Construtor que calcula automaticamente os metadados.
        /// </summary>
        /// <param name="data">Lista de itens</param>
        /// <param name="totalCount">Total de itens no banco</param>
        /// <param name="currentPage">Página atual</param>
        /// <param name="pageSize">Tamanho da página</param>
        public PagedResponse(IEnumerable<T> data, int totalCount, int currentPage, int pageSize)
        {
            Data = data;
            TotalCount = totalCount;
            CurrentPage = currentPage;
            PageSize = pageSize;
        }
    }
}
