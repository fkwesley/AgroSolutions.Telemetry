using Application.Interfaces;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    /// <summary>
    /// Handler responsável por processar o evento OrderStatusChangedEvent.
    /// Envia notificação de mudança de status da ordem.
    /// </summary>
    public class OrderStatusChangedEventHandler : IDomainEventHandler<OrderStatusChangedEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<OrderStatusChangedEventHandler> _logger;

        public OrderStatusChangedEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<OrderStatusChangedEventHandler> logger)
        {
            _publisherFactory = publisherFactory;
            _logger = logger;
        }

        public async Task HandleAsync(OrderStatusChangedEvent domainEvent)
        {
            _logger.LogInformation(
                "?? Processing OrderStatusChangedEvent | OrderId: {OrderId}, {OldStatus} ? {NewStatus}, OccurredOn: {OccurredOn}",
                domainEvent.OrderId,
                domainEvent.OldStatus,
                domainEvent.NewStatus,
                domainEvent.OccurredOn);

            // Publica notificação de mudança de status no RabbitMQ
            var rabbitMqPublisher = _publisherFactory.GetPublisher("RabbitMQ");
            await rabbitMqPublisher.PublishMessageAsync("fcg.notifications.queue", new
            {
                RequestId = domainEvent.OrderId,
                TemplateId = "OrderStatusChanged",
                Email = domainEvent.UserEmail,
                Parameters = new Dictionary<string, string>()
                {
                    { "{orderId}", domainEvent.OrderId.ToString() },
                    { "{newStatus}", domainEvent.NewStatus.ToString() }
                }
            });

            _logger.LogInformation(
                "? OrderStatusChangedEvent processed successfully | OrderId: {OrderId}",
                domainEvent.OrderId);
        }
    }
}
