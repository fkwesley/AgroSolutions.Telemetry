namespace Domain.Events
{
    /// <summary>
    /// Evento disparado quando uma ordem é criada.
    /// Contém todas as informações necessárias para notificações.
    /// </summary>
    public class OrderCreatedEvent : IDomainEvent
    {
        public int OrderId { get; }
        public string UserEmail { get; }
        public DateTime OccurredOn { get; }

        public OrderCreatedEvent(int orderId, string userEmail)
        {
            OrderId = orderId;
            UserEmail = userEmail;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
