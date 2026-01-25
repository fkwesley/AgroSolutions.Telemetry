using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.NewRelic.Logs;

namespace API.Configurations
{
    public static class LoggingConfiguration
    {
        /// <summary>
        /// Configura o Serilog com suporte a múltiplos sinks (Database, Elasticsearch, NewRelic, ou combinações)
        /// baseado na configuração do appsettings.json
        /// </summary>
        public static void ConfigureSerilog(IConfiguration configuration)
        {
            var loggerProvider = configuration["LoggerSettings:Provider"] ?? "Elastic";
            var serviceName = configuration["LoggerSettings:ServiceName"] ?? "orders-api";

            var environment = configuration["ElasticApm:Environment"] ?? "Production";
            var elasticUrl = configuration["ElasticLogs:Endpoint"];
            var elasticApiKey = configuration["ElasticLogs:ApiKey"];
            var elasticIndexPrefix = configuration["ElasticLogs:IndexPrefix"] ?? "app";

            var connectionString = configuration.GetConnectionString("FCGOrdersDbConnection");
            var newRelicEndpoint = configuration["NewRelic:Endpoint"];
            var newRelicLicenseKey = configuration["NewRelic:LicenseKey"];

            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext() // CRUCIAL: Permite enriquecer logs com LogId/CorrelationId
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", environment)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            // Configura sinks baseado no Provider (Database, Elastic, NewRelic, ou combinações)
            switch (loggerProvider.ToLower())
            {
                case "database":
                    ConfigureDatabaseSink(loggerConfig, connectionString);
                    break;

                case "elastic":
                    ConfigureElasticSink(loggerConfig, elasticUrl, elasticApiKey, elasticIndexPrefix);
                    break;

                case "newrelic":
                    if (!string.IsNullOrWhiteSpace(newRelicEndpoint))
                        ConfigureNewRelicSink(loggerConfig, newRelicEndpoint, newRelicLicenseKey, serviceName);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Invalid logger provider: {loggerProvider}. " +
                        $"Valid options: 'Database', 'Elastic', 'NewRelic', 'Database+Elastic' or 'Database+NewRelic'");
            }

            Log.Logger = loggerConfig.CreateLogger();
        }

        private static void ConfigureDatabaseSink(LoggerConfiguration loggerConfig, string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database connection string is required when using Database provider.");
            }

            loggerConfig.WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                {
                    TableName = "Trace_log",
                    SchemaName = "dbo",
                    AutoCreateSqlTable = true
                },
                columnOptions: GetColumnOptions());
        }

        private static void ConfigureElasticSink(LoggerConfiguration loggerConfig, string? elasticUrl, string? elasticApiKey, string elasticIndexPrefix)
        {
            if (string.IsNullOrWhiteSpace(elasticUrl))
            {
                throw new InvalidOperationException("Elasticsearch endpoint is required when using Elastic provider.");
            }

            loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUrl))
            {
                AutoRegisterTemplate = true,
                IndexFormat = $"{elasticIndexPrefix}-traces",
                CustomFormatter = new ExceptionAsObjectJsonFormatter(renderMessage: true),
                ModifyConnectionSettings = conn =>
                {
                    if (!string.IsNullOrWhiteSpace(elasticApiKey))
                    {
                        var credentials = new Elasticsearch.Net.ApiKeyAuthenticationCredentials(elasticApiKey);
                        conn.ApiKeyAuthentication(credentials);
                    }
                    return conn;
                },
                FailureCallback = (e, ex) => Console.WriteLine($"Failed to send log to Elasticsearch: {e?.MessageTemplate} - {ex?.Message}"),
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
            });
        }

        private static void ConfigureNewRelicSink(LoggerConfiguration loggerConfig, string endpointUrl, string? licenseKey, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                throw new InvalidOperationException("New Relic license key is required when using NewRelic provider.");
            
            loggerConfig.WriteTo.NewRelicLogs(
                endpointUrl: endpointUrl,
                applicationName: serviceName,
                licenseKey: licenseKey,
                insertKey: null,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                batchSizeLimit: 1000,
                period: TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Configuração de colunas customizadas para SQL Server
        /// Cria colunas específicas para LogId, CorrelationId, UserId e SourceContext
        /// em vez de armazenar tudo como JSON na coluna Properties
        /// </summary>
        private static Serilog.Sinks.MSSqlServer.ColumnOptions GetColumnOptions()
        {
            var columnOptions = new Serilog.Sinks.MSSqlServer.ColumnOptions();

            // Remove a coluna Properties padrão (vamos extrair campos específicos)
            columnOptions.Store.Remove(Serilog.Sinks.MSSqlServer.StandardColumn.Properties);

            // Adiciona colunas customizadas para LogId, CorrelationId, etc.
            columnOptions.AdditionalColumns = new System.Collections.ObjectModel.Collection<Serilog.Sinks.MSSqlServer.SqlColumn>
            {
                new Serilog.Sinks.MSSqlServer.SqlColumn
                {
                    ColumnName = "LogId",
                    DataType = System.Data.SqlDbType.NVarChar,
                    DataLength = 50,
                    AllowNull = true
                },
                new Serilog.Sinks.MSSqlServer.SqlColumn
                {
                    ColumnName = "CorrelationId",
                    DataType = System.Data.SqlDbType.NVarChar,
                    DataLength = 50,
                    AllowNull = true
                },
                new Serilog.Sinks.MSSqlServer.SqlColumn
                {
                    ColumnName = "UserId",
                    DataType = System.Data.SqlDbType.NVarChar,
                    DataLength = 100,
                    AllowNull = true
                },
                new Serilog.Sinks.MSSqlServer.SqlColumn
                {
                    ColumnName = "SourceContext",
                    DataType = System.Data.SqlDbType.NVarChar,
                    DataLength = 200,
                    AllowNull = true
                }
            };

            return columnOptions;
        }
    }
}
