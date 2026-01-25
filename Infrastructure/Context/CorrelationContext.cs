namespace Infrastructure.Context
{
    /// <summary>
    /// Gerencia o contexto de correlação (LogId e CorrelationId) usando AsyncLocal
    /// para preservar os valores através de chamadas assíncronas e threads.
    /// 
    /// ? FUNCIONA automaticamente em:
    /// - await/async em qualquer lugar
    /// - Task.WhenAll(), Task.WhenAny()
    /// - HttpClient, Entity Framework, Service Bus
    /// - Domain Event Handlers
    /// 
    /// ? NÃO funciona em:
    /// - Task.Run() sem capturar contexto antes
    /// - ThreadPool.QueueUserWorkItem()
    /// - Background jobs (Hangfire, Quartz)
    /// 
    /// Exemplo de uso com Task.Run():
    /// <code>
    /// var logId = CorrelationContext.LogId;
    /// var correlationId = CorrelationContext.CorrelationId;
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
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<CorrelationData> _data = new();

        /// <summary>
        /// LogId único para esta requisição HTTP específica.
        /// Gerado por esta API.
        /// </summary>
        public static Guid? LogId
        {
            get => _data.Value?.LogId;
            set
            {
                EnsureDataInitialized();
                _data.Value!.LogId = value;
            }
        }

        /// <summary>
        /// CorrelationId compartilhado entre múltiplas APIs na jornada do usuário.
        /// Propagado via header X-Correlation-Id ou gerado pela primeira API.
        /// </summary>
        public static Guid? CorrelationId
        {
            get => _data.Value?.CorrelationId;
            set
            {
                EnsureDataInitialized();
                _data.Value!.CorrelationId = value;
            }
        }

        /// <summary>
        /// Nome do serviço que gerou o log (da configuração).
        /// </summary>
        public static string? ServiceName
        {
            get => _data.Value?.ServiceName;
            set
            {
                EnsureDataInitialized();
                _data.Value!.ServiceName = value;
            }
        }

        /// <summary>
        /// UserId do usuário autenticado (se disponível).
        /// </summary>
        public static string? UserId
        {
            get => _data.Value?.UserId;
            set
            {
                EnsureDataInitialized();
                _data.Value!.UserId = value;
            }
        }

        /// <summary>
        /// Limpa o contexto atual (útil em testes).
        /// </summary>
        public static void Clear()
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
