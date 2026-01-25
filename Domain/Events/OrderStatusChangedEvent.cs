using Domain.Enums;

namespace Domain.Events
{
    /// <summary>
    /// Evento disparado quando o status de uma ordem muda.
    /// Contém transição de status e dados para notificação.
    /// </summary>
    public class OrderStatusChangedEvent : IDomainEvent
    {
        public int OrderId { get; }
        public OrderStatus OldStatus { get; }
        public OrderStatus NewStatus { get; }
        public string UserEmail { get; } 
        public DateTime OccurredOn { get; }

        public OrderStatusChangedEvent(int orderId, OrderStatus oldStatus, OrderStatus newStatus, string userEmail)
        {
            OrderId = orderId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            UserEmail = userEmail;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
