# AzureDotNet
The following repository will hold various .NET applications and tools hosted in Azure

## Solution Architecture

This solution consists of three projects working together:

### AzureDotNet.Shared.Configuration
Shared NuGet library providing centralized Azure Key Vault integration with Managed Identity.

Features:
- `AddAzureKeyVaultWithManagedIdentity()` extension for IConfigurationBuilder
- `IAzureConfigurationProvider` interface for strongly-typed configuration access
- DefaultAzureCredential for seamless authentication (local dev → Azure)
- Eliminates duplicate Key Vault setup code across projects

[View Shared Configuration Documentation](./AzureDotNet.Shared.Configuration/README.md)

### ServiceBusConsumerApp
Azure Function App that consumes messages from Azure Service Bus queues and topics.

Features:
- Queue message consumer
- Topic subscription message consumer
- Automatic message processing with error handling
- Dead letter queue support
- SendGrid email notifications
- Uses shared configuration library for Key Vault access

[View ServiceBusConsumerApp Documentation](./ServiceBusConsumerApp/README.md)

### ServiceBusProducerApp
Azure Function App that generates and sends synthetic messages to Azure Service Bus for testing.

Features:
- Generate realistic fake order messages with Bogus library
- Configurable message parameters (count, amounts, items)
- Support for queues and topics
- HTTP API endpoints
- Batch message sending
- Uses shared configuration library for Key Vault access

[View ServiceBusProducerApp Documentation](./ServiceBusProducerApp/README.md)

## Configuration

All secrets are stored in Azure Key Vault and accessed via Managed Identity:
- `ServiceBusConnection` - Service Bus namespace connection string
- `SendGridApiKey` - SendGrid API key for email notifications
- `SendGridFromEmail` - Default sender email address
- `SendGridFromName` - Default sender display name

Both function apps use the shared configuration library to eliminate code duplication.
