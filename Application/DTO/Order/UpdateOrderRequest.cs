using Domain.Enums;
using Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace Application.DTO.Order
{
    public class UpdateOrderRequest
    {
        [JsonIgnore]
        public int OrderId { get; set; }
        [JsonIgnore]
        public required string UserId { get; set; }
        public required OrderStatus Status { get; set; }
    }
}
