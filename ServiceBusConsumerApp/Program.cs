using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SendGrid;
using AzureDotNet.Shared.Configuration;
using ServiceBusConsumerApp.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure Azure Key Vault with Managed Identity using shared library
builder.Configuration.AddAzureKeyVaultWithManagedIdentity();

// Register Azure configuration provider
builder.Services.AddAzureConfiguration();

// Configure Application Insights
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] 
    ?? builder.Configuration["ApplicationInsightsConnectionString"];

if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
    builder.Services.ConfigureFunctionsApplicationInsights();
}
else
{
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService()
        .ConfigureFunctionsApplicationInsights();
}

// Register SendGrid client using configuration from Key Vault
builder.Services.AddSingleton<ISendGridClient>(sp =>
{
    var config = sp.GetRequiredService<IAzureConfigurationProvider>();
    return new SendGridClient(config.SendGridApiKey);
});

// Register EmailService
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Build().Run();
