using Domain.Events;

namespace Domain.Common
{
    // #SOLID - Single Responsibility Principle (SRP)
    // BaseEntity tem uma única responsabilidade: gerenciar eventos de domínio.
    // Não se preocupa com persistência, validação ou regras de negócio.
    
    // #SOLID - Open/Closed Principle (OCP)
    // Classe base aberta para extensão (herança) mas fechada para modificação.
    // Qualquer entidade pode herdar e adicionar eventos sem modificar BaseEntity.
    
    /// <summary>
    /// Classe base para entidades que suportam eventos de domínio
    /// </summary>
    public abstract class BaseEntity : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
