using Domain.Events;

namespace Application.Interfaces
{
    /// <summary>
    /// Interface para dispatcher de eventos de domínio.
    /// Permite inversão de dependência e facilita testes.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>
        /// Processa uma coleção de eventos de domínio.
        /// </summary>
        /// <param name="domainEvents">Os eventos a serem processados</param>
        Task ProcessAsync(IEnumerable<IDomainEvent> domainEvents);
    }
}
