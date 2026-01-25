using Application.Interfaces;
using Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    // #SOLID - Single Responsibility Principle (SRP)
    // DomainEventDispatcher tem uma única responsabilidade: despachar eventos para seus handlers.
    // Não conhece a lógica de processamento específica de cada evento.
    
    // #SOLID - Open/Closed Principle (OCP)
    // Novos eventos e handlers podem ser adicionados sem modificar o dispatcher.
    // O dispatcher usa reflexão e DI para encontrar handlers automaticamente.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Depende de IDomainEventHandler<T> (abstração) e usa IServiceProvider para resolver handlers.
    
    /// <summary>
    /// Dispatcher manual de eventos de domínio.
    /// Você controla QUANDO processar os eventos chamando ProcessAsync explicitamente.
    /// </summary>
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DomainEventDispatcher> _logger;

        public DomainEventDispatcher(
            IServiceProvider serviceProvider,
            ILogger<DomainEventDispatcher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Processa uma coleção de eventos de domínio.
        /// </summary>
        public async Task ProcessAsync(IEnumerable<IDomainEvent> domainEvents)
        {
            var eventsList = domainEvents.ToList();
            
            if (!eventsList.Any())
            {
                _logger.LogDebug("No domain events to process");
                return;
            }

            _logger.LogInformation("Processing {EventCount} domain event(s)", eventsList.Count);

            foreach (var domainEvent in eventsList)
                await ProcessEventAsync(domainEvent);

            _logger.LogInformation("All domain events processed successfully");
        }

        /// <summary>
        /// Processa um evento específico encontrando e executando seu handler.
        /// </summary>
        private async Task ProcessEventAsync(IDomainEvent domainEvent)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(EventHandlers.IDomainEventHandler<>).MakeGenericType(eventType);

            _logger.LogDebug(
                "Looking for handler: {HandlerType} for event: {EventType}",
                handlerType.Name,
                eventType.Name);

            // Busca o handler no DI
            var handler = _serviceProvider.GetService(handlerType);

            if (handler == null)
            {
                _logger.LogWarning(
                    "No handler found for event type: {EventType}. Event will be skipped.",
                    eventType.Name);
                return;
            }

            try
            {
                // Invoca o método HandleAsync do handler
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent });
                    if (task != null)
                    {
                        await task;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing event {EventType}",
                    eventType.Name);
                
                // Você pode decidir se quer:
                // 1. Lançar exceção (para o Service lidar)
                // 2. Continuar processando outros eventos
                // 3. Implementar retry logic
                
                throw; // Por enquanto, lança exceção
            }
        }
    }
}
