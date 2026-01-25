using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Factories
{
    // #SOLID - Open/Closed Principle (OCP)
    // Para adicionar um novo publisher (ex: Kafka, Azure Service Bus), basta:
    // 1. Criar a classe implementando IMessagePublisher
    // 2. Adicionar um novo case no switch
    // 3. Registrar no DI Container
    // Nenhum código cliente precisa ser modificado.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Factory retorna IMessagePublisher (abstração), não implementações concretas.
    public class MessagePublisherFactory : IMessagePublisherFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MessagePublisherFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMessagePublisher GetPublisher(string publisherType)
        {
            return publisherType switch
            {
                "RabbitMQ" => _serviceProvider.GetRequiredService<RabbitMQPublisher>(),
                "ServiceBus" => _serviceProvider.GetRequiredService<ServiceBusPublisher>(),
                _ => throw new ArgumentException($"Unknown publisher type: {publisherType}")
            };
        }
    }
}