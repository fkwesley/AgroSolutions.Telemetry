namespace Domain.ValueObjects
{
    /// <summary>
    /// Recomendação de irrigação calculada baseada em condições do solo e clima.
    /// </summary>
    public record IrrigationRecommendation(
        IrrigationUrgency Urgency,       // Urgência da irrigação
        decimal WaterAmountMM,            // Quantidade de água recomendada (mm)
        decimal CurrentMoisture,          // Umidade atual (%)
        decimal TargetMoisture,           // Umidade alvo (%)
        string Reason                     // Razão da recomendação
    )
    {
        /// <summary>
        /// Tempo estimado de irrigação (assumindo 1mm = 10 minutos).
        /// </summary>
        public TimeSpan EstimatedDuration => TimeSpan.FromMinutes((double)WaterAmountMM * 10);

        /// <summary>
        /// Déficit de umidade atual.
        /// </summary>
        public decimal MoistureDeficit => TargetMoisture - CurrentMoisture;
    }

    public enum IrrigationUrgency
    {
        None,       // Não precisa irrigar
        Low,        // Irrigar em 48-72h
        Medium,     // Irrigar em 24-48h
        High,       // Irrigar em 12-24h
        Critical    // Irrigar urgentemente (< 12h)
    }

    /// <summary>
    /// Resultado da análise de estresse térmico.
    /// </summary>
    public record HeatStressCondition(
        HeatStressLevel Level,           // Nível de estresse
        decimal AverageTemperature,      // Temperatura média no período
        decimal PeakTemperature,         // Temperatura máxima
        TimeSpan Duration,               // Duração do estresse
        DateTime StartTime               // Início do período de calor
    )
    {
        public double DurationInHours => Duration.TotalHours;
    }

    public enum HeatStressLevel
    {
        None,       // Temperatura OK
        Moderate,   // 30-35°C
        High,       // 35-40°C
        Severe      // > 40°C
    }

    /// <summary>
    /// Avaliação de risco de pragas baseada em condições climáticas.
    /// </summary>
    public record PestRiskAssessment(
        PestRiskLevel RiskLevel,         // Nível de risco
        int FavorableDaysCount,          // Dias consecutivos com condições favoráveis
        decimal AverageTemperature,      // Temperatura média
        decimal AverageMoisture,         // Umidade média
        string RiskFactors               // Fatores que contribuem para o risco
    );

    public enum PestRiskLevel
    {
        Minimal,    // Condições desfavoráveis para pragas
        Low,        // Poucas condições favoráveis
        Medium,     // Algumas condições favoráveis
        High,       // Muitas condições favoráveis
        Critical    // Condições ideais para proliferação
    }
}
