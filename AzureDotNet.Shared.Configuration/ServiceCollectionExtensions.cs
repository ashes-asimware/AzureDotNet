using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace AzureDotNet.Shared.Configuration
{
    /// <summary>
    /// Extension methods for registering Azure configuration services with dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers IAzureConfigurationProvider as a singleton service.
        /// This provides strongly-typed access to Azure configuration values.
        /// </summary>
        /// <param name="services">The IServiceCollection instance</param>
        /// <returns>The IServiceCollection for chaining</returns>
        public static IServiceCollection AddAzureConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<IAzureConfigurationProvider, AzureConfigurationProvider>();
            return services;
        }

        /// <summary>
        /// Registers IAzureConfigurationProvider with a specific IConfiguration instance.
        /// </summary>
        /// <param name="services">The IServiceCollection instance</param>
        /// <param name="configuration">The IConfiguration instance to use</param>
        /// <returns>The IServiceCollection for chaining</returns>
        public static IServiceCollection AddAzureConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<IAzureConfigurationProvider>(
                sp => new AzureConfigurationProvider(configuration));
            return services;
        }
    }
}
