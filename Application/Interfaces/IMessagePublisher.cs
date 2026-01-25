namespace Application.Interfaces
{
    // #SOLID - Interface Segregation Principle (ISP)
    // Interface específica para publicação de mensagens.
    // Implementações concretas (RabbitMQ, ServiceBus) não são forçadas a implementar métodos desnecessários.
    
    // #SOLID - Liskov Substitution Principle (LSP)
    // RabbitMQPublisher e ServiceBusPublisher podem ser substituídos entre si
    // pois ambos implementam esta interface com o mesmo contrato.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Abstração que permite trocar a tecnologia de mensageria sem impactar código cliente.
    public interface IMessagePublisher : IDisposable
    {
        Task PublishMessageAsync(string topicOrQueueName, object message, IDictionary<string, object>? customProperties = null);
    }
}