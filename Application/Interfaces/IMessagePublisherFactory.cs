namespace Application.Interfaces
{
    // #SOLID - Open/Closed Principle (OCP) + Dependency Inversion Principle (DIP)
    // Factory abstrata permite adicionar novos tipos de publishers sem modificar código existente.
    // Novos publishers podem ser criados implementando IMessagePublisher e registrando na factory.
    public interface IMessagePublisherFactory
    {
        IMessagePublisher GetPublisher(string publisherType);
    }
}