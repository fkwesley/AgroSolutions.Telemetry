using Application.Interfaces;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers
{
    // #SOLID - Single Responsibility Principle (SRP)
    // Este handler tem uma única responsabilidade: processar o evento OrderCreatedEvent.
    // Envia notificação quando um pedido é criado.
    
    // #SOLID - Open/Closed Principle (OCP)
    // Novos handlers para outros eventos podem ser criados sem modificar este.
    // Para adicionar nova funcionalidade ao evento OrderCreated, crie outro handler.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Depende de IMessagePublisherFactory (abstração), não de RabbitMQPublisher diretamente.
    
    /// <summary>
    /// Handler responsável por processar o evento OrderCreatedEvent.
    /// Envia notificação de confirmação de ordem criada.
    /// </summary>
    public class OrderCreatedEventHandler : IDomainEventHandler<OrderCreatedEvent>
    {
        private readonly IMessagePublisherFactory _publisherFactory;
        private readonly ILogger<OrderCreatedEventHandler> _logger;

        public OrderCreatedEventHandler(
            IMessagePublisherFactory publisherFactory,
            ILogger<OrderCreatedEventHandler> logger)
        {
            _publisherFactory = publisherFactory;
            _logger = logger;
        }

        public async Task HandleAsync(OrderCreatedEvent domainEvent)
        {
            _logger.LogInformation(
                "Processing OrderCreatedEvent | OrderId: {OrderId}, OccurredOn: {OccurredOn}",
                domainEvent.OrderId,
                domainEvent.OccurredOn);

            // Publica notificação no RabbitMQ
            var rabbitMqPublisher = _publisherFactory.GetPublisher("RabbitMQ");
            await rabbitMqPublisher.PublishMessageAsync("fcg.notifications.queue", new
            {
                RequestId = domainEvent.OrderId,
                TemplateId = "OrderReceived",
                Email = domainEvent.UserEmail, 
                Parameters = new Dictionary<string, string>()
                {
                    { "{orderId}", domainEvent.OrderId.ToString() }
                }
            });

            _logger.LogInformation("OrderCreatedEvent processed successfully | OrderId: {OrderId}", domainEvent.OrderId);
        }
    }
}
