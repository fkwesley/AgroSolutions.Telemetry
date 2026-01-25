using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Logging
{
    // #SOLID - Single Responsibility Principle (SRP)
    // DatabaseLoggerService tem uma única responsabilidade: persistir logs no banco de dados.
    
    // #SOLID - Liskov Substitution Principle (LSP)
    // Pode ser substituído por ElasticLoggerService ou NewRelicLoggerService
    // sem modificar o código cliente, pois todos implementam ILoggerService.
    
    // #SOLID - Dependency Inversion Principle (DIP)
    // Depende de IDatabaseLoggerRepository (abstração), não de implementação concreta.
    public class DatabaseLoggerService : ILoggerService
    {
        private readonly IDatabaseLoggerRepository _repository;
        private readonly ILogger<DatabaseLoggerService> _logger;

        public DatabaseLoggerService(
            IDatabaseLoggerRepository repository,
            ILogger<DatabaseLoggerService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LogRequestAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            try
            {
                await _repository.LogRequestAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log request to database for LogId: {LogId}", logEntry.LogId);
                throw;
            }
        }

        public async Task UpdateRequestLogAsync(RequestLog logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            try
            {
                await _repository.UpdateRequestLogAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update request log in database for LogId: {LogId}", logEntry.LogId);
                throw;
            }
        }
    }
}
