using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Domain Service responsável por recomendar irrigação baseada em condições do solo.
    /// 
    /// LÓGICA DE NEGÓCIO:
    /// - Analisa déficit hídrico comparando umidade atual vs. ideal
    /// - Considera tendência histórica (umidade está caindo ou subindo?)
    /// - Calcula quantidade de água necessária
    /// - Determina urgência baseada no déficit e tendência
    /// </summary>
    public interface IIrrigationRecommendationService
    {
        /// <summary>
        /// Analisa necessidade de irrigação baseada em medições recentes.
        /// Assume que measurements JÁ INCLUI a medição atual.
        /// </summary>
        /// <param name="measurements">Histórico incluindo a medição atual</param>
        /// <param name="optimalMoisture">Umidade ideal (%)</param>
        /// <param name="criticalMoisture">Umidade crítica (%)</param>
        /// <param name="soilCapacity">Capacidade de retenção do solo (mm)</param>
        /// <returns>Recomendação de irrigação ou null se não necessário</returns>
        IrrigationRecommendation? Analyze(
            IEnumerable<FieldMeasurement> measurements,
            decimal optimalMoisture,
            decimal criticalMoisture,
            decimal soilCapacity);
    }
}
