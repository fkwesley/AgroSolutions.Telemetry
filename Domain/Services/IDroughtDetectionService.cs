using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Domain Service responsável por detectar condições de seca.
    /// 
    /// Este é um Domain Service porque:
    /// 1. Contém lógica de negócio pura
    /// 2. Não cabe naturalmente em uma única entidade
    /// 3. Analisa múltiplas medições para tomar decisão
    /// 4. É reutilizável em diferentes contextos
    /// 5. Não depende de infraestrutura
    /// </summary>
    public interface IDroughtDetectionService
    {
        /// <summary>
        /// Detecta se há condição de seca baseado no histórico de medições.
        /// Assume que o histórico JÁ INCLUI a medição atual (última por data).
        /// </summary>
        /// <param name="measurements">Histórico de medições incluindo a atual</param>
        /// <param name="moistureThreshold">Limite de umidade do solo (%) abaixo do qual considera-se seca</param>
        /// <param name="minimumDurationHours">Duração mínima em horas que a umidade deve permanecer baixa</param>
        /// <returns>Condição de seca detectada ou null se não houver seca</returns>
        DroughtCondition? Detect(
            IEnumerable<FieldMeasurement> measurements,
            decimal moistureThreshold,
            int minimumDurationHours);
    }
}
