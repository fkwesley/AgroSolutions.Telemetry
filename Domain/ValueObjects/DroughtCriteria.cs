namespace Domain.ValueObjects
{
    /// <summary>
    /// Critérios para detecção de condição de seca.
    /// Value Object imutável que encapsula os parâmetros de detecção.
    /// </summary>
    public record DroughtCriteria(
        decimal Threshold,           // Limiar de umidade do solo (%)
        int MinimumDurationHours     // Duração mínima em horas
    )
    {
        /// <summary>
        /// Valida se os critérios são válidos.
        /// </summary>
        public void Validate()
        {
            if (Threshold < 0 || Threshold > 100)
                throw new ArgumentException("Threshold deve estar entre 0 e 100%.", nameof(Threshold));
            
            if (MinimumDurationHours <= 0)
                throw new ArgumentException("MinimumDurationHours deve ser maior que 0.", nameof(MinimumDurationHours));
        }
    }
}
