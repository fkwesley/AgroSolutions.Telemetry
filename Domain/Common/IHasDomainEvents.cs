using Domain.Events;

namespace Domain.Common
{
    /// <summary>
    /// Interface para entidades que possuem eventos de domínio
    /// </summary>
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void AddDomainEvent(IDomainEvent domainEvent);
        void RemoveDomainEvent(IDomainEvent domainEvent);
        void ClearDomainEvents();
    }
}
