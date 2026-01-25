using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Events
{
    /// <summary>
    /// Evento disparado quando o método de pagamento é definido.
    /// Contém todas as informações necessárias para processamento de pagamento.
    /// </summary>
    public class PaymentMethodSetEvent : IDomainEvent
    {
        public int OrderId { get; }
        public PaymentMethod PaymentMethod { get; }
        public double TotalPrice { get; }  
        public string UserEmail { get; }
        public PaymentMethodDetails? PaymentDetails { get; }  
        public DateTime OccurredOn { get; }

        public PaymentMethodSetEvent(
            int orderId, 
            PaymentMethod paymentMethod,
            double totalPrice,
            string userEmail,
            PaymentMethodDetails? paymentDetails)
        {
            OrderId = orderId;
            PaymentMethod = paymentMethod;
            TotalPrice = totalPrice;
            UserEmail = userEmail;
            PaymentDetails = paymentDetails;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
