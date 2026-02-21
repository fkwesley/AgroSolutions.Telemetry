namespace Application.Settings
{
    /// <summary>
    /// Configurações gerais para análise de condições do campo.
    /// </summary>
    public class AnalysisSettings
    {
        /// <summary>
        /// Número de dias de histórico a buscar do banco (única query).
        /// Default: 30 dias
        /// </summary>
        public int HistoryDays { get; set; } = 30;
    }

    /// <summary>
    /// Configurações para detecção de chuva excessiva.
    /// </summary>
    public class ExcessiveRainfallSettings
    {
        /// <summary>
        /// Limite de precipitação (mm) considerado excessivo.
        /// Default: 60mm
        /// </summary>
        public decimal Threshold { get; set; } = 60m;
    }

    /// <summary>
    /// Configurações para detecção de calor extremo.
    /// </summary>
    public class ExtremeHeatSettings
    {
        /// <summary>
        /// Temperatura (°C) considerada extremamente alta.
        /// Default: 40°C
        /// </summary>
        public decimal Threshold { get; set; } = 40m;
    }

    /// <summary>
    /// Configurações para detecção de temperatura de congelamento.
    /// </summary>
    public class FreezingTemperatureSettings
    {
        /// <summary>
        /// Temperatura (°C) considerada de congelamento.
        /// Default: 0°C
        /// </summary>
        public decimal Threshold { get; set; } = 0m;
    }

    /// <summary>
    /// Configurações para detecção de seca.
    /// </summary>
    public class DroughtAlertSettings
    {
        /// <summary>
        /// Limite mínimo de umidade do solo (%) abaixo do qual uma condição de seca é considerada.
        /// Default: 30%
        /// </summary>
        public decimal Threshold { get; set; } = 30m;

        /// <summary>
        /// Duração mínima (em horas) que a umidade deve permanecer abaixo do limite para acionar um alerta.
        /// Default: 24 horas
        /// </summary>
        public int MinimumDurationHours { get; set; } = 24;

        /// <summary>
        /// Dias de histórico necessários para análise de tendência de seca.
        /// Default: 7 dias
        /// </summary>
        public int HistoryDays { get; set; } = 7;
    }

    /// <summary>
    /// Configurações para recomendação de irrigação.
    /// </summary>
    public class IrrigationSettings
    {
        /// <summary>
        /// Umidade do solo ideal (%) - acima disso não precisa irrigar.
        /// Default: 60%
        /// </summary>
        public decimal OptimalMoisture { get; set; } = 60m;

        /// <summary>
        /// Umidade crítica (%) - abaixo disso é urgente irrigar.
        /// Default: 30%
        /// </summary>
        public decimal CriticalMoisture { get; set; } = 30m;

        /// <summary>
        /// Capacidade de retenção de água do solo (mm).
        /// Default: 150mm (solo médio)
        /// </summary>
        public decimal SoilWaterCapacity { get; set; } = 150m;

        /// <summary>
        /// Dias de histórico necessários para análise de tendência.
        /// Default: 7 dias
        /// </summary>
        public int HistoryDays { get; set; } = 7;
    }

    /// <summary>
    /// Configurações para análise de estresse térmico.
    /// </summary>
    public class HeatStressSettings
    {
        /// <summary>
        /// Temperatura crítica (°C) - acima disso há estresse térmico severo.
        /// Default: 35°C
        /// </summary>
        public decimal CriticalTemperature { get; set; } = 35m;

        /// <summary>
        /// Duração mínima de calor intenso para alerta (horas).
        /// Default: 6 horas
        /// </summary>
        public int MinimumDurationHours { get; set; } = 6;

        /// <summary>
        /// Horas de histórico necessários para análise.
        /// Default: 24 horas (1 dia)
        /// </summary>
        public int HistoryHours { get; set; } = 24;
    }

    /// <summary>
    /// Configurações para análise de risco de pragas.
    /// </summary>
    public class PestRiskSettings
    {
        /// <summary>
        /// Temperatura mínima ideal para proliferação de pragas (°C).
        /// Default: 22°C
        /// </summary>
        public decimal MinTemperature { get; set; } = 22m;

        /// <summary>
        /// Temperatura máxima ideal para proliferação de pragas (°C).
        /// Default: 32°C
        /// </summary>
        public decimal MaxTemperature { get; set; } = 32m;

        /// <summary>
        /// Umidade mínima favorável para pragas (%).
        /// Default: 60%
        /// </summary>
        public decimal MinMoisture { get; set; } = 60m;

        /// <summary>
        /// Dias consecutivos com condições favoráveis para alerta.
        /// Default: 5 dias
        /// </summary>
        public int MinimumFavorableDays { get; set; } = 5;

        /// <summary>
        /// Dias de histórico necessários para análise de tendências.
        /// Default: 14 dias
        /// </summary>
        public int HistoryDays { get; set; } = 14;
    }
}
