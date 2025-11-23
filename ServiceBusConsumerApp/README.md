# Service Bus Consumer Function App

This Azure Function App consumes messages from Azure Service Bus queues and topics.

## Features

- **ServiceBusConsumerFunction**: Consumes messages from a Service Bus queue
- **ServiceBusTopicConsumerFunction**: Consumes messages from a Service Bus topic subscription

## Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools
- Azure Service Bus namespace with a queue named `myqueue` and/or a topic named `mytopic` with subscription `mysubscription`

## Configuration

Update the `local.settings.json` file with your Azure Service Bus connection string:

```json
{
  "Values": {
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"
  }
}
```

Or use Azure Key Vault reference in production:
```json
{
  "Values": {
    "ServiceBusConnection": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/ServiceBusConnectionString/)"
  }
}
```

## Running Locally

1. Install dependencies:
   ```bash
   dotnet restore
   ```

2. Run the function app:
   ```bash
   func start
   ```

## Deployment to Azure

1. Create a Function App in Azure:
   ```bash
   az functionapp create --resource-group <resource-group> \
     --name <function-app-name> \
     --storage-account <storage-account> \
     --consumption-plan-location <location> \
     --runtime dotnet-isolated \
     --functions-version 4
   ```

2. Configure the Service Bus connection:
   ```bash
   az functionapp config appsettings set --name <function-app-name> \
     --resource-group <resource-group> \
     --settings ServiceBusConnection="<your-connection-string>"
   ```

3. Deploy the function:
   ```bash
   func azure functionapp publish <function-app-name>
   ```

## Message Processing

The functions automatically:
- Receive messages from Service Bus
- Log message details
- Process the message
- Handle exceptions (failed messages go to dead letter queue)
- Auto-complete successfully processed messages

## Customization

To customize the queue/topic names, update the `ServiceBusTrigger` attribute:

```csharp
[ServiceBusTrigger("your-queue-name", Connection = "ServiceBusConnection")]
```

For topics:
```csharp
[ServiceBusTrigger("your-topic-name", "your-subscription-name", Connection = "ServiceBusConnection")]
```

## Error Handling

- Exceptions thrown in the function will cause the message to be retried
- After max delivery count, messages move to the dead letter queue
- Configure retry policies in `host.json` if needed
