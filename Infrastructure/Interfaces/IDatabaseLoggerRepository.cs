using Domain.Entities;

namespace Infrastructure.Interfaces
{
    public interface IDatabaseLoggerRepository
    {
        Task LogRequestAsync(RequestLog log);
        Task UpdateRequestLogAsync(RequestLog log);
    }
}
