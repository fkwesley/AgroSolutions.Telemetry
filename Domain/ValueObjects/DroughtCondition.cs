namespace Domain.ValueObjects
{
    /// <summary>
    /// Representa uma condição de seca detectada.
    /// Value Object imutável que encapsula os detalhes da seca.
    /// </summary>
    public record DroughtCondition(
        DateTime StartTime,    // Quando a condição de seca começou
        TimeSpan Duration      // Duração da seca
    )
    {
        /// <summary>
        /// Data/hora final da condição de seca.
        /// </summary>
        public DateTime EndTime => StartTime.Add(Duration);

        /// <summary>
        /// Duração em horas (para facilitar logging/display).
        /// </summary>
        public double DurationInHours => Duration.TotalHours;
    }
}
