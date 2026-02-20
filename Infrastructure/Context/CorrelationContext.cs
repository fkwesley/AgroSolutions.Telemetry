using Application.Interfaces;

namespace Infrastructure.Context
{
    /// <summary>
    /// Manages correlation context (LogId and CorrelationId) using AsyncLocal
    /// to preserve values across asynchronous calls and threads.
    /// 
    /// ✅ WORKS automatically in:
    /// - await/async anywhere
    /// - Task.WhenAll(), Task.WhenAny()
    /// - HttpClient, Entity Framework, Service Bus
    /// - Domain Event Handlers
    /// 
    /// ❌ DOES NOT work in:
    /// - Task.Run() without capturing context first
    /// - ThreadPool.QueueUserWorkItem()
    /// - Background jobs (Hangfire, Quartz) without explicit propagation
    /// 
    /// Example usage with Task.Run():
    /// <code>
    /// var logId = _correlationContext.LogId;
    /// var correlationId = _correlationContext.CorrelationId;
    /// 
    /// _ = Task.Run(async () => 
    /// {
    ///     using (Serilog.Context.LogContext.PushProperty("LogId", logId))
    ///     using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    ///     {
    ///         await DoWorkAsync();
    ///     }
    /// });
    /// </code>
    /// </summary>
    public class CorrelationContext : ICorrelationContext
    {
        private static readonly AsyncLocal<CorrelationData> _data = new();

        public Guid? LogId
        {
            get => _data.Value?.LogId;
            set
            {
                EnsureDataInitialized();
                _data.Value!.LogId = value;
            }
        }

        public Guid? CorrelationId
        {
            get => _data.Value?.CorrelationId;
            set
            {
                EnsureDataInitialized();
                _data.Value!.CorrelationId = value;
            }
        }

        public string? ServiceName
        {
            get => _data.Value?.ServiceName;
            set
            {
                EnsureDataInitialized();
                _data.Value!.ServiceName = value;
            }
        }

        public string? UserId
        {
            get => _data.Value?.UserId;
            set
            {
                EnsureDataInitialized();
                _data.Value!.UserId = value;
            }
        }

        public void Clear()
        {
            _data.Value = null!;
        }

        private static void EnsureDataInitialized()
        {
            _data.Value ??= new CorrelationData();
        }

        private class CorrelationData
        {
            public Guid? LogId { get; set; }
            public Guid? CorrelationId { get; set; }
            public string? ServiceName { get; set; }
            public string? UserId { get; set; }
        }
    }
}
