# Application Insights Setup Guide

This guide explains how to configure Azure Application Insights for the ServiceBusConsumerApp and ServiceBusProducerApp.

## Overview

Both function apps are configured to send telemetry data to Azure Application Insights, including:
- Function execution logs
- Performance metrics
- Dependency tracking (Service Bus, SendGrid, Key Vault)
- Custom events and traces
- HTTP request tracking
- Live metrics
- Distributed tracing (W3C standard)

## Prerequisites

1. Azure subscription
2. Azure Application Insights resource
3. Azure Key Vault (for storing the connection string)

## Setup Steps

### 1. Create Application Insights Resource

```bash
# Set variables
RESOURCE_GROUP="your-resource-group"
LOCATION="eastus"
APP_INSIGHTS_NAME="your-appinsights-name"

# Create Application Insights
az monitor app-insights component create \
  --app $APP_INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Get the connection string
az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv
```

### 2. Store Connection String in Azure Key Vault

```bash
# Set variables
KEY_VAULT_NAME="your-keyvault-name"
CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=..."

# Add the connection string to Key Vault
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ApplicationInsightsConnectionString" \
  --value "$CONNECTION_STRING"
```

### 3. Configure Local Development

For local development, update the `local.settings.json` in each app:

**ServiceBusConsumerApp/local.settings.json:**
```json
{
    "Values": {
        "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=...;IngestionEndpoint=..."
    }
}
```

**ServiceBusProducerApp/local.settings.json:**
```json
{
    "Values": {
        "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=...;IngestionEndpoint=..."
    }
}
```

### 4. Configure Azure Function App

When deploying to Azure, configure the Application Insights connection:

#### Option A: Using Azure Portal
1. Go to your Function App in Azure Portal
2. Navigate to Settings → Configuration
3. Add Application Setting:
   - Name: `APPLICATIONINSIGHTS_CONNECTION_STRING`
   - Value: Reference from Key Vault: `@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/ApplicationInsightsConnectionString/)`

#### Option B: Using Azure CLI
```bash
FUNCTION_APP_NAME="your-function-app"
RESOURCE_GROUP="your-resource-group"

# Get Key Vault reference
KEY_VAULT_REFERENCE="@Microsoft.KeyVault(SecretUri=https://${KEY_VAULT_NAME}.vault.azure.net/secrets/ApplicationInsightsConnectionString/)"

# Set the application setting
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$KEY_VAULT_REFERENCE"
```

### 5. Enable Managed Identity (if not already enabled)

The Function App needs Managed Identity to access Key Vault:

```bash
# Enable system-assigned managed identity
az functionapp identity assign \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

## Configuration Details

### host.json Configuration

Both apps have enhanced Application Insights configuration in `host.json`:

```json
{
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true,
      "enableDependencyTracking": true,
      "enablePerformanceCountersCollection": true,
      "httpAutoCollectionOptions": {
        "enableHttpTriggerExtendedInfoCollection": true,
        "enableW3CDistributedTracing": true,
        "enableResponseHeaderInjection": true
      }
    },
    "logLevel": {
      "default": "Information",
      "Function": "Information"
    }
  }
}
```

### Features Enabled

1. **Sampling**: Limits telemetry to 20 items/second to control costs
2. **Live Metrics**: Real-time monitoring in Azure Portal
3. **Dependency Tracking**: Automatic tracking of calls to Service Bus, SendGrid, Key Vault
4. **Performance Counters**: CPU, memory, and other system metrics
5. **Distributed Tracing**: W3C standard for request correlation across services
6. **HTTP Extended Info**: Detailed HTTP request/response data

### Log Levels

- **Information**: Standard operational logs
- **Warning**: Potential issues or anomalies
- **Error**: Execution failures and exceptions
- **Trace**: Detailed debugging information

## Monitoring in Azure Portal

### View Telemetry

1. Go to your Application Insights resource in Azure Portal
2. Navigate to:
   - **Live Metrics**: Real-time telemetry stream
   - **Performance**: Function execution times and dependencies
   - **Failures**: Exception tracking and failure analysis
   - **Logs**: Query telemetry using KQL (Kusto Query Language)
   - **Application Map**: Visualize dependencies and call flows

### Example Queries

**Function Execution Logs:**
```kusto
traces
| where cloud_RoleName contains "ServiceBusConsumer" or cloud_RoleName contains "ServiceBusProducer"
| where timestamp > ago(1h)
| order by timestamp desc
```

**Function Performance:**
```kusto
requests
| where cloud_RoleName contains "ServiceBusConsumer" or cloud_RoleName contains "ServiceBusProducer"
| summarize avg(duration), percentile(duration, 95), percentile(duration, 99) by name
```

**Exception Tracking:**
```kusto
exceptions
| where cloud_RoleName contains "ServiceBusConsumer" or cloud_RoleName contains "ServiceBusProducer"
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
```

**Dependency Calls:**
```kusto
dependencies
| where cloud_RoleName contains "ServiceBusConsumer" or cloud_RoleName contains "ServiceBusProducer"
| summarize count(), avg(duration) by type, target, name
```

## Shared Configuration Library

The `AzureDotNet.Shared.Configuration` library now includes Application Insights support:

```csharp
public interface IAzureConfigurationProvider
{
    string? ApplicationInsightsConnectionString { get; }
    // ... other properties
}
```

The connection string is automatically loaded from:
1. `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable (standard)
2. `ApplicationInsightsConnectionString` from Key Vault

## Cost Optimization

Application Insights charges are based on data ingestion. To optimize costs:

1. **Sampling**: Configured to limit telemetry rate (20 items/second)
2. **Exclude Request Telemetry**: Reduces redundant data
3. **Log Level Filtering**: Set appropriate log levels to reduce noise
4. **Data Retention**: Configure retention period in Azure Portal (default 90 days)

### Estimated Costs (as of 2025)
- First 5 GB/month: Free
- Additional data: ~$2.30/GB
- Data retention beyond 90 days: $0.10/GB/month

Typical monthly costs for moderate workload: $10-50

## Troubleshooting

### No Data Appearing in Application Insights

1. Verify connection string is correct
2. Check Function App has access to Key Vault
3. Ensure `APPLICATIONINSIGHTS_CONNECTION_STRING` is set
4. Check host.json is properly formatted
5. Restart the Function App

### High Costs

1. Review sampling settings in host.json
2. Adjust log levels to reduce verbosity
3. Check for excessive custom telemetry
4. Review Application Insights pricing tier

### Missing Dependency Tracking

1. Ensure `enableDependencyTracking` is true in host.json
2. Verify Application Insights SDK version is up to date
3. Check that dependencies are using supported clients

## Additional Resources

- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Azure Functions Monitoring](https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring)
- [KQL Query Language](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/)
- [Application Insights Pricing](https://azure.microsoft.com/en-us/pricing/details/monitor/)
