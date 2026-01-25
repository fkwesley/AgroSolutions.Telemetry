namespace Domain.Events
{
    // #SOLID - Interface Segregation Principle (ISP)
    // Interface mínima que define apenas o que é essencial para um evento de domínio.
    // Implementações concretas adicionam propriedades específicas conforme necessário.
    
    // #SOLID - Open/Closed Principle (OCP)
    // Novos eventos podem ser criados implementando esta interface sem modificá-la.
    
    /// <summary>
    /// Marker interface para eventos de domínio
    /// </summary>
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
