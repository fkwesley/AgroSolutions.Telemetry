using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Implementação da lógica de detecção de condição de seca.
    /// 
    /// REGRA DE NEGÓCIO:
    /// Uma seca é detectada quando:
    /// 1. A umidade do solo fica abaixo do threshold definido
    /// 2. Esta condição persiste de forma CONTÍNUA por um período mínimo
    /// 3. Se houver uma medição com umidade OK, o contador reseta
    /// 
    /// EXEMPLO:
    /// - Threshold: 30%
    /// - Duração mínima: 24h
    /// - Medições: [25%, 28%, 22%, 35%, 20%, 18%]
    /// - Resultado: NÃO dispara (sequência quebrada pelo 35%)
    /// </summary>
    public class DroughtDetectionService : IDroughtDetectionService
    {
        public DroughtCondition? Detect(
            IEnumerable<FieldMeasurement> measurements,
            decimal moistureThreshold,
            int minimumDurationHours)
        {
            // Validar parâmetros
            if (moistureThreshold < 0 || moistureThreshold > 100)
                throw new ArgumentException("Threshold deve estar entre 0 e 100%.", nameof(moistureThreshold));

            if (minimumDurationHours <= 0)
                throw new ArgumentException("MinimumDurationHours deve ser maior que 0.", nameof(minimumDurationHours));

            var orderedMeasurements = measurements
                .OrderBy(m => m.CollectedAt)
                .ToList();

            // Precisa de pelo menos 2 medições para análise
            if (orderedMeasurements.Count < 2)
                return null;

            // Pegar a medição mais recente (última)
            var current = orderedMeasurements.Last();

            // Early return: umidade atual está OK
            if (current.SoilMoisture >= moistureThreshold)
                return null;

            // Encontrar o início da sequência CONTÍNUA de baixa umidade
            DateTime? droughtStartTime = null;

            foreach (var measurement in orderedMeasurements)
            {
                if (measurement.SoilMoisture < moistureThreshold)
                {
                    // Primeira medição abaixo do threshold
                    droughtStartTime ??= measurement.CollectedAt;
                }
                else
                {
                    // Medição OK quebra a sequência - resetar
                    droughtStartTime = null;
                }
            }

            // Verificar se há sequência contínua que atende a duração mínima
            if (droughtStartTime.HasValue)
            {
                var duration = current.CollectedAt - droughtStartTime.Value;

                if (duration.TotalHours >= minimumDurationHours)
                {
                    return new DroughtCondition(droughtStartTime.Value, duration);
                }
            }

            return null;
        }
    }
}
