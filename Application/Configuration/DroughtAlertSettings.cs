namespace Application.Configuration
{
    /// <summary>
    /// Configuration settings for drought alert detection.
    /// </summary>
    public class DroughtAlertSettings
    {
        /// <summary>
        /// Minimum soil moisture threshold (%) below which a drought condition is considered.
        /// Default: 30%
        /// </summary>
        public decimal Threshold { get; set; } = 30m;

        /// <summary>
        /// Minimum duration (in hours) that moisture must stay below threshold to trigger an alert.
        /// Default: 24 hours
        /// </summary>
        public int MinimumDurationHours { get; set; } = 24;

        /// <summary>
        /// Days of history needed for drought trend analysis.
        /// Default: 7 days
        /// </summary>
        public int HistoryDays { get; set; } = 7;
    }
}
