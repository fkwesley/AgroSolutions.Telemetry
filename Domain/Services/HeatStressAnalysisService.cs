using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Implementação da análise de estresse térmico.
    /// 
    /// REGRA DE NEGÓCIO:
    /// Estresse térmico ocorre quando:
    /// 1. Temperatura sustentada acima de 35°C
    /// 2. Duração >= 6 horas contínuas
    /// 3. Afeta fotossíntese e desenvolvimento das plantas
    /// </summary>
    public class HeatStressAnalysisService : IHeatStressAnalysisService
    {
        public HeatStressCondition? Analyze(
            IEnumerable<FieldMeasurement> measurements,
            decimal criticalTemperature,
            int minimumHours)
        {
            var orderedMeasurements = measurements
                .OrderBy(m => m.CollectedAt)
                .ToList();

            if (orderedMeasurements.Count < 2)
                return null;

            // Pegar a medição mais recente (atual)
            var current = orderedMeasurements.Last();

            // Se temperatura atual está OK, não há estresse
            if (current.AirTemperature < criticalTemperature)
                return null;

            // Encontrar sequência contínua de calor intenso
            DateTime? stressStartTime = null;
            decimal peakTemperature = 0;
            decimal sumTemperature = 0;
            int countAboveThreshold = 0;

            foreach (var measurement in orderedMeasurements)
            {
                if (measurement.AirTemperature >= criticalTemperature)
                {
                    stressStartTime ??= measurement.CollectedAt;
                    peakTemperature = Math.Max(peakTemperature, measurement.AirTemperature);
                    sumTemperature += measurement.AirTemperature;
                    countAboveThreshold++;
                }
                else
                {
                    // Reset ao encontrar temperatura normal
                    stressStartTime = null;
                    peakTemperature = 0;
                    sumTemperature = 0;
                    countAboveThreshold = 0;
                }
            }

            // Verificar se duração atende o mínimo
            if (stressStartTime.HasValue)
            {
                var duration = current.CollectedAt - stressStartTime.Value;

                if (duration.TotalHours >= minimumHours)
                {
                    var avgTemperature = sumTemperature / countAboveThreshold;
                    var level = DetermineStressLevel(avgTemperature);

                    return new HeatStressCondition(
                        Level: level,
                        AverageTemperature: avgTemperature,
                        PeakTemperature: peakTemperature,
                        Duration: duration,
                        StartTime: stressStartTime.Value
                    );
                }
            }

            return null;
        }

        private HeatStressLevelEnum DetermineStressLevel(decimal avgTemperature)
        {
            return avgTemperature switch
            {
                >= 40 => HeatStressLevelEnum.Severe,
                >= 37 => HeatStressLevelEnum.High,
                >= 35 => HeatStressLevelEnum.Moderate,
                _ => HeatStressLevelEnum.None
            };
        }
    }
}
