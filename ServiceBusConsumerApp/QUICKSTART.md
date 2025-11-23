# Quick Start Guide

## Overview

This Service Bus Consumer Function App provides multiple examples of consuming messages from Azure Service Bus:

1. **Basic Queue Consumer** (`ServiceBusConsumerFunction.cs`)
   - Simple string message processing
   - Queue and Topic subscription examples

2. **Advanced Consumer** (`AdvancedServiceBusConsumer.cs`)
   - Typed message handling with `ServiceBusReceivedMessage`
   - Access to message properties and metadata
   - Batch processing support
   - Example business logic implementation

## Quick Start

### 1. Configure Connection

Edit `local.settings.json`:
```json
{
  "Values": {
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"
  }
}
```

### 2. Create Service Bus Resources

Create these queues in Azure Service Bus:
- `myqueue` - for basic consumer
- `orders-queue` - for advanced consumer
- `batch-queue` - for batch processing

Optional topic:
- `mytopic` with subscription `mysubscription`

### 3. Run Locally

```bash
cd ServiceBusConsumerApp
func start
```

### 4. Send Test Message

Using Azure Portal or Azure CLI:
```bash
az servicebus queue send \
  --resource-group <rg> \
  --namespace-name <namespace> \
  --name myqueue \
  --body "Hello World"
```

### 5. Watch Logs

You should see output like:
```
[INFO] Processing message from Service Bus queue
[INFO] Message Body: Hello World
[INFO] Message processed successfully
```

## Functions Included

| Function Name | Trigger | Description |
|--------------|---------|-------------|
| ServiceBusConsumerFunction | Queue: `myqueue` | Basic string message consumer |
| ServiceBusTopicConsumerFunction | Topic: `mytopic` | Topic subscription consumer |
| ProcessOrderMessages | Queue: `orders-queue` | Advanced typed message consumer |
| ProcessBatchMessages | Queue: `batch-queue` | Batch message processing |

## Next Steps

1. **Customize for your use case**: Update queue/topic names in the function attributes
2. **Add your message models**: Create classes in the `Models` folder
3. **Implement business logic**: Add your processing logic in the function methods
4. **Configure settings**: Adjust `host.json` for performance tuning
5. **Deploy to Azure**: Use `func azure functionapp publish <app-name>`

## Key Files

- `ServiceBusConsumerFunction.cs` - Basic consumer examples
- `AdvancedServiceBusConsumer.cs` - Advanced patterns
- `Models/OrderMessage.cs` - Example message model
- `host.json` - Service Bus configuration
- `local.settings.json` - Local development settings
- `README.md` - Detailed documentation
- `TESTING.md` - Testing guide

## Common Patterns

### Simple String Messages
```csharp
[Function("MyFunction")]
public void Run([ServiceBusTrigger("queue-name", Connection = "ServiceBusConnection")] string message)
{
    _logger.LogInformation($"Message: {message}");
}
```

### Typed Messages with Metadata
```csharp
[Function("MyFunction")]
public void Run([ServiceBusTrigger("queue-name", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
{
    var myData = JsonSerializer.Deserialize<MyType>(message.Body.ToString());
    _logger.LogInformation($"Message ID: {message.MessageId}");
    _logger.LogInformation($"Custom Property: {message.ApplicationProperties["MyProperty"]}");
}
```

### Batch Processing
```csharp
[Function("MyFunction")]
public void Run([ServiceBusTrigger("queue-name", Connection = "ServiceBusConnection", IsBatched = true)] ServiceBusReceivedMessage[] messages)
{
    foreach (var message in messages)
    {
        // Process each message
    }
}
```

## Support

For more details, see:
- [README.md](./README.md) - Full documentation
- [TESTING.md](./TESTING.md) - Testing guide
- [Azure Functions Service Bus Documentation](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus)
