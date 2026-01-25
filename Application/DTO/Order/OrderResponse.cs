using Application.DTO.Common;
using Application.DTO.Game;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace Application.DTO.Order
{
    /// <summary>
    /// Resposta de pedido com suporte a HATEOAS.
    /// Inclui links para navegar entre recursos relacionados.
    /// </summary>
    public class OrderResponse : IHateoasResource
    {
        public int OrderId { get; set; }
        public required string UserId { get; set; }
        public required string UserEmail { get; set; }
        public IEnumerable<GameResponse> ListOfGames { get; set; } = new List<GameResponse>(); 
        public required OrderStatus Status { get; set; }
        public required PaymentMethod PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public double TotalPrice { get; set; }

        /// <summary>
        /// Links HATEOAS para navegação.
        /// Aparece como "_links" no JSON.
        /// </summary>
        [JsonPropertyName("_links")]
        public List<Link> Links { get; set; } = new();
    }
}

