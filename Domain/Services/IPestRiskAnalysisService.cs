using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Domain Service responsável por avaliar risco de pragas.
    /// 
    /// LÓGICA DE NEGÓCIO:
    /// - Identifica condições climáticas favoráveis para proliferação de pragas
    /// - Combina temperatura + umidade do solo
    /// - Conta dias consecutivos com condições ideais
    /// </summary>
    public interface IPestRiskAnalysisService
    {
        /// <summary>
        /// Analisa risco de pragas baseado em condições climáticas.
        /// Assume que measurements JÁ INCLUI a medição atual.
        /// </summary>
        /// <param name="measurements">Histórico incluindo a medição atual</param>
        /// <param name="minTemperature">Temperatura mínima favorável (°C)</param>
        /// <param name="maxTemperature">Temperatura máxima favorável (°C)</param>
        /// <param name="minMoisture">Umidade mínima favorável (%)</param>
        /// <param name="minimumDays">Dias mínimos consecutivos para alerta</param>
        /// <returns>Avaliação de risco de pragas ou null</returns>
        PestRiskAssessment? Analyze(
            IEnumerable<FieldMeasurement> measurements,
            decimal minTemperature,
            decimal maxTemperature,
            decimal minMoisture,
            int minimumDays);
    }
}
