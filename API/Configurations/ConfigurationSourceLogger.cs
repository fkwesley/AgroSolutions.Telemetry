using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Serilog;

namespace API.Configurations;

/// <summary>
/// Utilitário que loga a origem de cada valor de configuração sensível/importante.
/// Permite identificar se o valor veio de: Azure Key Vault, Environment Variable ou appsettings.
/// </summary>
public static class ConfigurationSourceLogger
{
    /// <summary>
    /// Chaves de configuração monitoradas, agrupadas por categoria.
    /// </summary>
    private static readonly Dictionary<string, string[]> MonitoredKeys = new()
    {
        ["ConnectionStrings"] =
        [
            "ConnectionStrings:TelemetryDbConnection",
            "ConnectionStrings:ServiceBusConnection"
        ],
        ["JWT"] =
        [
            "Jwt:Key",
            "Jwt:Issuer"
        ],
        ["ElasticLogs"] =
        [
            "ElasticLogs:Endpoint",
            "ElasticLogs:ApiKey",
            "ElasticLogs:IndexPrefix"
        ],
        ["ElasticApm"] =
        [
            "ElasticApm:Enabled",
            "ElasticApm:ServerUrl",
            "ElasticApm:SecretToken",
            "ElasticApm:ServiceName",
            "ElasticApm:Environment"
        ],
        ["LoggerSettings"] =
        [
            "LoggerSettings:Provider",
            "LoggerSettings:ServiceName"
        ],
        ["CosmosDb"] =
        [
            "CosmosDb:DatabaseId"
        ],
        ["KeyVault"] =
        [
            "KeyVault:VaultUri",
            "KeyVault:Enabled"
        ]
    };

    /// <summary>
    /// Loga a origem de cada configuração monitorada no startup da aplicação em um único trace.
    /// Identifica o provedor que forneceu o valor final (último a vencer):
    ///   - AzureKeyVault → valor veio do Key Vault
    ///   - EnvironmentVariables → valor veio de variável de ambiente
    ///   - JsonConfigurationProvider (appsettings.{env}.json) → valor veio do appsettings de ambiente
    ///   - JsonConfigurationProvider (appsettings.json) → valor veio do appsettings base
    ///   - [NOT SET] → chave não está configurada em nenhum provedor
    /// </summary>
    public static void LogConfigurationSources(IConfigurationRoot configRoot)
    {
        var configEntries = new List<Dictionary<string, string>>();
        var missingKeys = new List<string>();

        foreach (var (category, keys) in MonitoredKeys)
        {
            foreach (var key in keys)
            {
                var (source, hasValue) = ResolveSource(configRoot, key);
                var isSensitive = IsSensitiveKey(key);
                var value = configRoot[key];

                var maskedValue = hasValue
                    ? (isSensitive ? MaskValue(value) : value)
                    : "[NOT SET]";

                configEntries.Add(new Dictionary<string, string>
                {
                    ["Category"] = category,
                    ["Key"] = key,
                    ["Source"] = source,
                    ["Value"] = maskedValue ?? "[NOT SET]"
                });

                if (!hasValue)
                    missingKeys.Add(key);
            }
        }

        Log.Information(
            "Configuration Source Diagnostics (Priority: KeyVault > EnvVars > appsettings.env.json > appsettings.json) " +
            "{@ConfigEntries} {MissingKeysCount} {MissingKeys}",
            configEntries,
            missingKeys.Count,
            missingKeys);
    }

    /// <summary>
    /// Resolve qual provedor de configuração forneceu o valor final para a chave.
    /// Percorre os provedores de trás para frente (último adicionado = maior prioridade).
    /// </summary>
    private static (string Source, bool HasValue) ResolveSource(IConfigurationRoot configRoot, string key)
    {
        // Percorre os provedores na ordem reversa (último = maior prioridade)
        var providers = configRoot.Providers.Reverse().ToList();

        foreach (var provider in providers)
        {
            if (provider.TryGet(key, out _))
            {
                var sourceName = GetProviderName(provider);
                return (sourceName, true);
            }
        }

        return ("NOT SET", false);
    }

    /// <summary>
    /// Retorna um nome amigável para o provedor de configuração.
    /// </summary>
    private static string GetProviderName(IConfigurationProvider provider)
    {
        return provider switch
        {
            // Azure Key Vault
            AzureKeyVaultConfigurationProvider => "🔐 AzureKeyVault",

            // Environment Variables
            Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationProvider
                => "🌍 EnvironmentVariable",

            // JSON files (appsettings.json, appsettings.Development.json, etc.)
            Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider jsonProvider
                => $"📄 {Path.GetFileName(jsonProvider.Source.Path ?? "json")}",

            // Command line
            Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationProvider
                => "⌨️ CommandLine",

            // Fallback: nome completo do tipo
            _ => $"❓ {provider.GetType().Name}"
        };
    }

    /// <summary>
    /// Determina se uma chave contém informação sensível que deve ser mascarada no log.
    /// </summary>
    private static bool IsSensitiveKey(string key)
    {
        var sensitivePatterns = new[]
        {
            "ConnectionString", "Connection", "Key", "Secret", "Token", "ApiKey", "Password"
        };

        return sensitivePatterns.Any(p => key.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Mascara um valor sensível, exibindo apenas os primeiros 4 caracteres.
    /// </summary>
    private static string MaskValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "[EMPTY]";

        if (value.Length <= 4)
            return "****";

        return $"{value[..4]}****";
    }
}
