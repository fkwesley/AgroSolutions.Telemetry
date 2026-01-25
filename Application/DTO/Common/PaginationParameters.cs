using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Application.DTO.Common
{
    /// <summary>
    /// Parâmetros de paginação recebidos via query string.
    /// Exemplo: ?page=1&pageSize=10
    /// </summary>
    public class PaginationParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        /// <summary>
        /// Número da página (começa em 1).
        /// Default: 1
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Quantidade de itens por página.
        /// Min: 1, Max: 100, Default: 10
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        /// <summary>
        /// Calcula quantos itens pular no banco de dados.
        /// Fórmula: (Page - 1) * PageSize
        /// Exemplo: Page=3, PageSize=10 ? Skip=20
        /// </summary>
        [JsonIgnore]  
        [FromQuery(Name = "")] 
        [Browsable(false)]  // ?? NOVO - Oculta de ferramentas de design
        [EditorBrowsable(EditorBrowsableState.Never)]  // ?? NOVO - Oculta do IntelliSense
        public int Skip => (Page - 1) * PageSize;

        /// <summary>
        /// Quantos itens retornar (alias para PageSize).
        /// </summary>
        [JsonIgnore]  
        [FromQuery(Name = "")] 
        [Browsable(false)]  // ?? NOVO
        [EditorBrowsable(EditorBrowsableState.Never)]  // ?? NOVO
        public int Take => PageSize;
    }
}
