namespace Application.EventHandlers
{
    // #SOLID - Interface Segregation Principle (ISP)
    // Interface genérica e específica para cada tipo de evento.
    // Handlers implementam apenas a interface para o evento que processam.
    
    // #SOLID - Single Responsibility Principle (SRP)
    // Cada handler tem uma única responsabilidade: processar um tipo específico de evento.
    
    // #SOLID - Open/Closed Principle (OCP)
    // Novos handlers podem ser criados sem modificar código existente.
    // Basta implementar esta interface para um novo tipo de evento.
    
    /// <summary>
    /// Interface para handlers de eventos de domínio.
    /// Cada handler é responsável por processar um tipo específico de evento.
    /// </summary>
    public interface IDomainEventHandler<in TEvent> where TEvent : Domain.Events.IDomainEvent
    {
        /// <summary>
        /// Processa o evento de domínio.
        /// O evento deve conter todas as informações necessárias para o processamento.
        /// </summary>
        /// <param name="domainEvent">O evento a ser processado com todos os dados necessários</param>
        Task HandleAsync(TEvent domainEvent);
    }
}
