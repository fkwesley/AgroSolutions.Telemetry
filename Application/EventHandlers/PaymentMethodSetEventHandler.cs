using Application.Interfaces;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Handler responsável por processar o evento PaymentMethodSetEvent.
    /// Envia informações de pagamento para o Service Bus.
    /// </summary>
    public class PaymentMethodSetEventHandler : IDomainEventHandler<PaymentMethodSetEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<PaymentMethodSetEventHandler> _logger;

        public PaymentMethodSetEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<PaymentMethodSetEventHandler> logger)
        {
            _publisherFactory = publisherFactory;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentMethodSetEvent domainEvent)
        {
            _logger.LogInformation(
                "?? Processing PaymentMethodSetEvent | OrderId: {OrderId}, PaymentMethod: {PaymentMethod}, OccurredOn: {OccurredOn}",
                domainEvent.OrderId,
                domainEvent.PaymentMethod,
                domainEvent.OccurredOn);

            // Publica evento de pagamento no Service Bus
            var serviceBusPublisher = _publisherFactory.GetPublisher("ServiceBus");
            await serviceBusPublisher.PublishMessageAsync("fcg.paymentstopic", new
            {
                OrderId = domainEvent.OrderId,
                amount = domainEvent.TotalPrice,
                PaymentMethod = domainEvent.PaymentMethod,
                CardNumber = domainEvent.PaymentDetails?.CardNumber,
                CardHolder = domainEvent.PaymentDetails?.CardHolder,
                ExpiryDate = domainEvent.PaymentDetails?.ExpiryDate,
                Cvv = domainEvent.PaymentDetails?.Cvv,
                Email = domainEvent.UserEmail,
            },
            new Dictionary<string, object> {
                {"PaymentMethod", domainEvent.PaymentMethod.ToString() }
            });

            _logger.LogInformation(
                "? PaymentMethodSetEvent processed successfully | OrderId: {OrderId}",
                domainEvent.OrderId);
        }
    }
}
