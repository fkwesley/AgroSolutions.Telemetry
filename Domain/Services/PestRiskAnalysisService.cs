using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Implementação da análise de risco de pragas.
    /// 
    /// REGRA DE NEGÓCIO:
    /// Risco alto de pragas quando:
    /// 1. Temperatura na faixa ideal para insetos/fungos (22-32°C)
    /// 2. Umidade do solo alta (> 60%) indica umidade do ar alta também
    /// 3. Condições persistem por vários dias consecutivos (>= 5 dias)
    /// 
    /// PRAGAS COMUNS FAVORECIDAS:
    /// - Lagartas: 25-30°C
    /// - Pulgões: 22-27°C
    /// - Fungos patogênicos: 20-30°C + alta umidade
    /// </summary>
    public class PestRiskAnalysisService : IPestRiskAnalysisService
    {
        public PestRiskAssessment? Analyze(
            IEnumerable<FieldMeasurement> measurements,
            decimal minTemperature,
            decimal maxTemperature,
            decimal minMoisture,
            int minimumDays)
        {
            var orderedMeasurements = measurements
                .OrderBy(m => m.CollectedAt)
                .ToList();

            if (orderedMeasurements.Count < 2)
                return null;

            // Agrupar por dia e calcular médias diárias
            var dailyConditions = orderedMeasurements
                .GroupBy(m => m.CollectedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgTemperature = g.Average(m => m.AirTemperature),
                    AvgMoisture = g.Average(m => m.SoilMoisture),
                    IsFavorable = g.Average(m => m.AirTemperature) >= minTemperature &&
                                  g.Average(m => m.AirTemperature) <= maxTemperature &&
                                  g.Average(m => m.SoilMoisture) >= minMoisture
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Contar dias consecutivos favoráveis
            int maxConsecutiveDays = 0;
            int currentConsecutiveDays = 0;

            foreach (var day in dailyConditions)
            {
                if (day.IsFavorable)
                {
                    currentConsecutiveDays++;
                    maxConsecutiveDays = Math.Max(maxConsecutiveDays, currentConsecutiveDays);
                }
                else
                {
                    currentConsecutiveDays = 0;
                }
            }

            // Se não há dias consecutivos suficientes, risco é baixo
            if (maxConsecutiveDays < minimumDays)
            {
                // Retornar avaliação de risco baixo se houver pelo menos 2 dias favoráveis
                if (maxConsecutiveDays >= 2)
                {
                    var recentAvgTemp = dailyConditions.Average(d => d.AvgTemperature);
                    var recentAvgMoisture = dailyConditions.Average(d => d.AvgMoisture);

                    return new PestRiskAssessment(
                        RiskLevel: PestRiskLevel.Low,
                        FavorableDaysCount: maxConsecutiveDays,
                        AverageTemperature: recentAvgTemp,
                        AverageMoisture: recentAvgMoisture,
                        RiskFactors: $"Apenas {maxConsecutiveDays} dias consecutivos com condições favoráveis"
                    );
                }

                return null;
            }

            // Calcular médias do período
            var avgTemperature = dailyConditions.Average(d => d.AvgTemperature);
            var avgMoisture = dailyConditions.Average(d => d.AvgMoisture);

            // Determinar nível de risco
            var riskLevel = DetermineRiskLevel(maxConsecutiveDays, avgTemperature, avgMoisture, minMoisture);

            // Identificar fatores de risco
            var riskFactors = IdentifyRiskFactors(maxConsecutiveDays, avgTemperature, avgMoisture, minTemperature, maxTemperature);

            return new PestRiskAssessment(
                RiskLevel: riskLevel,
                FavorableDaysCount: maxConsecutiveDays,
                AverageTemperature: avgTemperature,
                AverageMoisture: avgMoisture,
                RiskFactors: riskFactors
            );
        }

        private PestRiskLevel DetermineRiskLevel(
            int consecutiveDays,
            decimal avgTemp,
            decimal avgMoisture,
            decimal minMoisture)
        {
            // Quanto mais dias consecutivos + temperatura ideal + umidade alta = maior risco
            var score = 0;

            // Dias consecutivos (peso maior)
            if (consecutiveDays >= 10) score += 4;
            else if (consecutiveDays >= 7) score += 3;
            else if (consecutiveDays >= 5) score += 2;

            // Temperatura ideal para pragas (25-28°C é ótimo)
            if (avgTemp >= 25 && avgTemp <= 28) score += 3;
            else if (avgTemp >= 22 && avgTemp <= 32) score += 2;
            else if (avgTemp >= 20 && avgTemp <= 35) score += 1;

            // Umidade alta favorece fungos e insetos
            if (avgMoisture >= 70) score += 3;
            else if (avgMoisture >= minMoisture) score += 2;

            return score switch
            {
                >= 9 => PestRiskLevel.Critical,
                >= 7 => PestRiskLevel.High,
                >= 5 => PestRiskLevel.Medium,
                >= 3 => PestRiskLevel.Low,
                _ => PestRiskLevel.Minimal
            };
        }

        private string IdentifyRiskFactors(
            int days,
            decimal temp,
            decimal moisture,
            decimal minTemp,
            decimal maxTemp)
        {
            var factors = new List<string>();

            if (days >= 7)
                factors.Add($"{days} dias consecutivos com condições favoráveis");

            if (temp >= 25 && temp <= 28)
                factors.Add($"temperatura ideal para pragas ({temp:F1}°C)");
            else if (temp >= minTemp && temp <= maxTemp)
                factors.Add($"temperatura favorável ({temp:F1}°C)");

            if (moisture >= 70)
                factors.Add($"umidade muito alta ({moisture:F1}%)");
            else if (moisture >= 60)
                factors.Add($"umidade alta ({moisture:F1}%)");

            return string.Join(", ", factors);
        }
    }
}
