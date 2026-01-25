using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using Domain.Common;
using Domain.Events;
using System.Diagnostics;
using System.Globalization;

namespace Domain.Entities
{
    // #SOLID - Single Responsibility Principle (SRP)
    // A entidade Order é responsável por:
    // 1. Manter o estado do pedido
    // 2. Encapsular regras de negócio (validações de cartão, mudanças de status)
    // 3. Gerenciar eventos de domínio
    // Ela NÃO é responsável por persistência, logging ou comunicação externa.
    
    // #SOLID - Open/Closed Principle (OCP)
    // Novos eventos de domínio podem ser adicionados sem modificar a estrutura base da entidade.
    // A validação de regras de negócio está aberta para extensão (novos métodos) mas fechada para modificação.
    [DebuggerDisplay("OrderId: {OrderId}, UserId: {UserId}, ListOfGames: {ListOfGames.Count}, Status: {Status}")]
    public class Order : BaseEntity
    {
        public int OrderId { get; set; }
        public required string UserId { get; set; }
        public required string UserEmail { get; set; }
        public ICollection<Game> ListOfGames { get; set; } = new List<Game>(); // Propriedade de navegação para os jogos selecionados
        public required OrderStatus Status
        {
            get => _status;
            set
            {
                // #SOLID - Single Responsibility Principle (SRP)
                // O setter encapsula a lógica de mudança de status e disparo de eventos.
                // A entidade é responsável por garantir sua consistência.
                var currentStatus = _status;
                var newStatus = value;

                if (currentStatus == OrderStatus.Released)
                    throw new BusinessException("Cannot change the status of an order that is already released.");

                _status = newStatus;
                
                // #SOLID - Open/Closed Principle (OCP)
                // Eventos são adicionados dinamicamente. Novos handlers podem processar esses eventos
                // sem modificar a entidade Order.
                if(newStatus != currentStatus && OrderId != 0)
                    AddDomainEvent(new OrderStatusChangedEvent(OrderId, currentStatus, newStatus, UserEmail));
            }
        }
        private OrderStatus _status;

        public required PaymentMethod PaymentMethod { get; set; }
        private PaymentMethodDetails? _paymentMethodDetails { get; set; } = null; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public double TotalPrice 
        { 
            get 
            { 
                if (ListOfGames == null || !ListOfGames.Any()) 
                    return 0; 
                
                return ListOfGames.Sum(game => game.Price); 
            }
        }

        // #SOLID - Single Responsibility Principle (SRP)
        // Cada método de validação tem uma única responsabilidade clara.
        // Regra de negócio para exigir detalhes do método de pagamento se for cartão de crédito
        public PaymentMethodDetails? PaymentMethodDetails
        {
            get => _paymentMethodDetails;
            set
            {
                if (PaymentMethod != PaymentMethod.Pix && value == null)
                    throw new BusinessException("Payment method details are required for credit or debit card payments.");

                if (value != null)
                {
                    if (!IsValidCardNumber(value.Value.CardNumber))
                        throw new BusinessException("Invalid card number.");

                    if (!IsValidExpiryDate(value.Value.ExpiryDate))
                        throw new BusinessException("The card has already expired or is invalid. Provide a new card");
                }

                _paymentMethodDetails = value;
            }
        }

        public bool IsValidCardNumber(string cardNumber)
        {
            return !string.IsNullOrWhiteSpace(cardNumber) && cardNumber.Length >= 13 && cardNumber.Length <= 19;
        }

        public bool IsValidExpiryDate(string expiryDate)
        {
            //returns true if the expiry date is in the future
            if (DateTime.TryParseExact(expiryDate, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                // Considera o último dia do mês como válido
                var lastDayOfMonth = new DateTime(parsedDate.Year, parsedDate.Month, DateTime.DaysInMonth(parsedDate.Year, parsedDate.Month));
                return lastDayOfMonth >= DateTime.UtcNow.Date;
            }
            else
                return false;
        }

        /// <summary>
        /// Marca a ordem como criada, disparando o evento de domínio.
        /// </summary>
        public void MarkAsCreated()
        {
            AddDomainEvent(new OrderCreatedEvent(OrderId, UserEmail));
            AddDomainEvent(new PaymentMethodSetEvent(OrderId, PaymentMethod, TotalPrice, UserEmail, _paymentMethodDetails));
        }

    }
}