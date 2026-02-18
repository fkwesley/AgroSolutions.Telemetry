using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Implementação do serviço de recomendação de irrigação.
    /// 
    /// ALGORITMO:
    /// 1. Calcular déficit de umidade (ideal - atual)
    /// 2. Analisar tendência dos últimos 7 dias (secando ou melhorando?)
    /// 3. Determinar urgência baseada em déficit + tendência
    /// 4. Calcular quantidade de água necessária
    /// </summary>
    public class IrrigationRecommendationService : IIrrigationRecommendationService
    {
        public IrrigationRecommendation? Analyze(
            IEnumerable<FieldMeasurement> measurements,
            decimal optimalMoisture,
            decimal criticalMoisture,
            decimal soilCapacity)
        {
            var orderedMeasurements = measurements
                .OrderBy(m => m.CollectedAt)
                .ToList();

            if (orderedMeasurements.Count < 2)
                return null;

            // Pegar a medição mais recente (atual)
            var current = orderedMeasurements.Last();

            // Se umidade está boa, não irrigar
            if (current.SoilMoisture >= optimalMoisture)
                return null;

            // Calcular déficit
            var deficit = optimalMoisture - current.SoilMoisture;

            // Analisar tendência (está secando ou melhorando?)
            var trend = CalculateMoistureTrend(orderedMeasurements);

            // Determinar urgência
            var urgency = DetermineUrgency(current.SoilMoisture, criticalMoisture, deficit, trend);

            // Se urgência é None, não precisa irrigar ainda
            if (urgency == IrrigationUrgency.None)
                return null;

            // Calcular quantidade de água
            var waterAmount = CalculateWaterAmount(deficit, soilCapacity);

            // Gerar razão
            var reason = GenerateReason(deficit, trend, urgency);

            return new IrrigationRecommendation(
                Urgency: urgency,
                WaterAmountMM: waterAmount,
                CurrentMoisture: current.SoilMoisture,
                TargetMoisture: optimalMoisture,
                Reason: reason
            );
        }

        private decimal CalculateMoistureTrend(List<FieldMeasurement> measurements)
        {
            if (measurements.Count < 2)
                return 0;

            // Comparar média dos últimos 2 dias vs. média dos 2 dias anteriores
            var recent = measurements.TakeLast(Math.Min(10, measurements.Count / 2)).Average(m => m.SoilMoisture);
            var older = measurements.Take(Math.Min(10, measurements.Count / 2)).Average(m => m.SoilMoisture);

            return recent - older; // Negativo = secando, Positivo = melhorando
        }

        private IrrigationUrgency DetermineUrgency(
            decimal currentMoisture, 
            decimal criticalMoisture,
            decimal deficit,
            decimal trend)
        {
            // Crítico: abaixo do threshold crítico
            if (currentMoisture <= criticalMoisture)
                return IrrigationUrgency.Critical;

            // Alto: déficit grande OU tendência de queda acentuada
            if (deficit > 20 || (deficit > 10 && trend < -3))
                return IrrigationUrgency.High;

            // Médio: déficit moderado
            if (deficit > 10 || (deficit > 5 && trend < -2))
                return IrrigationUrgency.Medium;

            // Baixo: déficit pequeno mas presente
            if (deficit > 5)
                return IrrigationUrgency.Low;

            return IrrigationUrgency.None;
        }

        private decimal CalculateWaterAmount(decimal deficit, decimal soilCapacity)
        {
            // Converter déficit percentual para mm baseado na capacidade do solo
            // Ex: déficit de 20% em solo com capacidade 150mm = 30mm de água
            return (deficit / 100m) * soilCapacity;
        }

        private string GenerateReason(decimal deficit, decimal trend, IrrigationUrgency urgency)
        {
            var trendText = trend switch
            {
                < -3 => "em queda acentuada",
                < -1 => "em queda",
                > 1 => "em recuperação",
                _ => "estável"
            };

            return urgency switch
            {
                IrrigationUrgency.Critical => $"Umidade crítica com déficit de {deficit:F1}%, tendência {trendText}",
                IrrigationUrgency.High => $"Déficit hídrico alto ({deficit:F1}%), tendência {trendText}",
                IrrigationUrgency.Medium => $"Déficit moderado ({deficit:F1}%), tendência {trendText}",
                IrrigationUrgency.Low => $"Déficit leve ({deficit:F1}%), monitorar próximas 48h",
                _ => ""
            };
        }
    }
}
