using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace AzureDotNet.Shared.Configuration
{
    /// <summary>
    /// Extension methods for configuring Azure Key Vault with IConfigurationBuilder
    /// </summary>
    public static class KeyVaultConfigurationExtensions
    {
        /// <summary>
        /// Adds Azure Key Vault configuration provider with Managed Identity authentication.
        /// Reads the Key Vault URI from configuration key "KeyVault:Uri".
        /// </summary>
        /// <param name="builder">The IConfigurationBuilder instance</param>
        /// <param name="keyVaultUriKey">Configuration key for Key Vault URI (default: "KeyVault:Uri")</param>
        /// <returns>The IConfigurationBuilder for chaining</returns>
        public static IConfigurationBuilder AddAzureKeyVaultWithManagedIdentity(
            this IConfigurationBuilder builder,
            string keyVaultUriKey = "KeyVault:Uri")
        {
            // Build intermediate configuration to read Key Vault URI
            var config = builder.Build();
            var keyVaultUri = config[keyVaultUriKey];

            if (string.IsNullOrEmpty(keyVaultUri))
            {
                // Key Vault not configured - this is okay for local dev with direct config
                return builder;
            }

            // DefaultAzureCredential tries multiple authentication methods:
            // 1. Environment variables (for local dev)
            // 2. Managed Identity (for Azure deployment)
            // 3. Visual Studio, Azure CLI, Azure PowerShell (for local dev)
            var credential = new DefaultAzureCredential();
            var secretClient = new SecretClient(new Uri(keyVaultUri), credential);

            builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

            return builder;
        }

        /// <summary>
        /// Adds Azure Key Vault configuration provider with explicit URI.
        /// </summary>
        /// <param name="builder">The IConfigurationBuilder instance</param>
        /// <param name="keyVaultUri">The Key Vault URI</param>
        /// <returns>The IConfigurationBuilder for chaining</returns>
        public static IConfigurationBuilder AddAzureKeyVaultWithManagedIdentity(
            this IConfigurationBuilder builder,
            Uri keyVaultUri)
        {
            if (keyVaultUri == null)
            {
                throw new ArgumentNullException(nameof(keyVaultUri));
            }

            var credential = new DefaultAzureCredential();
            var secretClient = new SecretClient(keyVaultUri, credential);

            builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

            return builder;
        }
    }
}
