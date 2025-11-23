using Microsoft.Extensions.Configuration;

namespace AzureDotNet.Shared.Configuration
{
    /// <summary>
    /// Provides strongly-typed access to Azure configuration values from Key Vault and app settings.
    /// </summary>
    public interface IAzureConfigurationProvider
    {
        string ServiceBusConnection { get; }
        string SendGridApiKey { get; }
        string SendGridFromEmail { get; }
        string SendGridFromName { get; }
        string? ApplicationInsightsConnectionString { get; }
        string GetSecret(string key);
        string? GetSecretOrDefault(string key, string? defaultValue = null);
    }

    /// <summary>
    /// Implementation of IAzureConfigurationProvider that reads from IConfiguration.
    /// Configuration can come from Key Vault, app settings, or local settings.
    /// </summary>
    public class AzureConfigurationProvider : IAzureConfigurationProvider
    {
        private readonly IConfiguration _configuration;

        public AzureConfigurationProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets the Service Bus connection string or fully qualified namespace.
        /// Checks both "ServiceBusConnection" and "ServiceBusNamespace" keys.
        /// Returns connection string for local dev or fully qualified namespace for Managed Identity in Azure.
        /// </summary>
        public string ServiceBusConnection
        {
            get
            {
                // Try full connection string first (for local development)
                var connectionString = _configuration["ServiceBusConnection"];
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return connectionString;
                }

                // Try fully qualified namespace for Managed Identity (Azure production)
                var namespace_ = _configuration["ServiceBusNamespace"];
                if (!string.IsNullOrEmpty(namespace_))
                {
                    return namespace_;
                }

                // Try alternative key
                var fqn = _configuration["ServiceBusConnection__fullyQualifiedNamespace"];
                if (!string.IsNullOrEmpty(fqn))
                {
                    return fqn;
                }

                throw new InvalidOperationException(
                    "ServiceBusConnection configuration is missing. " +
                    "Ensure either 'ServiceBusConnection' (connection string) or 'ServiceBusNamespace' (fully qualified namespace for Managed Identity) is configured in Key Vault or app settings.");
            }
        }

        /// <summary>
        /// Gets the SendGrid API key from configuration.
        /// </summary>
        public string SendGridApiKey
        {
            get
            {
                var apiKey = _configuration["SendGridApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException(
                        "SendGridApiKey configuration is missing. " +
                        "Ensure 'SendGridApiKey' is configured in Key Vault or app settings.");
                }
                return apiKey;
            }
        }

        /// <summary>
        /// Gets the SendGrid sender email address from configuration.
        /// </summary>
        public string SendGridFromEmail
        {
            get
            {
                var email = _configuration["SendGridFromEmail"];
                if (string.IsNullOrEmpty(email))
                {
                    throw new InvalidOperationException(
                        "SendGridFromEmail configuration is missing. " +
                        "Ensure 'SendGridFromEmail' is configured in Key Vault or app settings.");
                }
                return email;
            }
        }

        /// <summary>
        /// Gets the SendGrid sender display name from configuration.
        /// </summary>
        public string SendGridFromName
        {
            get
            {
                var name = _configuration["SendGridFromName"];
                if (string.IsNullOrEmpty(name))
                {
                    throw new InvalidOperationException(
                        "SendGridFromName configuration is missing. " +
                        "Ensure 'SendGridFromName' is configured in Key Vault or app settings.");
                }
                return name;
            }
        }

        /// <summary>
        /// Gets the Application Insights connection string from configuration.
        /// Returns null if not configured (Application Insights is optional).
        /// </summary>
        public string? ApplicationInsightsConnectionString
        {
            get
            {
                // Try standard environment variable first
                var connectionString = _configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return connectionString;
                }

                // Try Key Vault format
                connectionString = _configuration["ApplicationInsightsConnectionString"];
                return string.IsNullOrEmpty(connectionString) ? null : connectionString;
            }
        }

        /// <summary>
        /// Gets a secret value from configuration by key.
        /// Throws an exception if the key is not found.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>The configuration value</returns>
        /// <exception cref="InvalidOperationException">Thrown when the key is not found</exception>
        public string GetSecret(string key)
        {
            var value = _configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                    $"Configuration key '{key}' is missing. " +
                    $"Ensure '{key}' is configured in Key Vault or app settings.");
            }
            return value;
        }

        /// <summary>
        /// Gets a secret value from configuration by key, or returns a default value if not found.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is not found</param>
        /// <returns>The configuration value or default value</returns>
        public string? GetSecretOrDefault(string key, string? defaultValue = null)
        {
            var value = _configuration[key];
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}
