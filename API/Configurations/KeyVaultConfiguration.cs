using Azure.Identity;

namespace API.Configurations;

/// <summary>
/// Configura o Azure Key Vault como provedor de configuração.
/// Ordem de prioridade (último adicionado vence):
///   1. appsettings.json (menor prioridade)
///   2. appsettings.{Environment}.json
///   3. Environment Variables
///   4. Azure Key Vault (maior prioridade)
/// </summary>
public static class KeyVaultConfiguration
{
    /// <summary>
    /// Adiciona Azure Key Vault ao pipeline de configuração.
    /// Usa DefaultAzureCredential para autenticação, que suporta:
    ///   - Managed Identity (AKS/Azure)
    ///   - Azure CLI (desenvolvimento local)
    ///   - Environment Variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
    /// </summary>
    public static IConfigurationBuilder AddKeyVaultConfiguration(this IConfigurationBuilder builder)
    {
        // Faz build intermediário para ler a URI do Key Vault das fontes já registradas
        var config = builder.Build();

        var vaultUri = config["KeyVault:VaultUri"];
        var enabled = config.GetValue<bool>("KeyVault:Enabled");

        if (!enabled || string.IsNullOrWhiteSpace(vaultUri))
        {
            return builder;
        }

        builder.AddAzureKeyVault(
            new Uri(vaultUri),
            new DefaultAzureCredential());

        return builder;
    }
}
