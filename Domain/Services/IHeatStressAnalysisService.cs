using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Domain Service responsável por detectar estresse térmico em plantas.
    /// 
    /// LÓGICA DE NEGÓCIO:
    /// - Identifica períodos prolongados de calor intenso
    /// - Calcula duração e severidade do estresse
    /// - Considera temperatura sustentada, não apenas picos momentâneos
    /// </summary>
    public interface IHeatStressAnalysisService
    {
        /// <summary>
        /// Analisa condições de estresse térmico.
        /// Assume que measurements JÁ INCLUI a medição atual.
        /// </summary>
        /// <param name="measurements">Histórico incluindo a medição atual</param>
        /// <param name="criticalTemperature">Temperatura crítica (°C)</param>
        /// <param name="minimumHours">Duração mínima para alerta (horas)</param>
        /// <returns>Condição de estresse térmico ou null</returns>
        HeatStressCondition? Analyze(
            IEnumerable<FieldMeasurement> measurements,
            decimal criticalTemperature,
            int minimumHours);
    }
}
