using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AzureDotNet.Shared.Configuration;
using ServiceBusProducerApp.Services;

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

// Register services
builder.Services.AddSingleton<IMessageGenerator, MessageGenerator>();
builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();

builder.Build().Run();
