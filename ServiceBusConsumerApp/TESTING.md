# Testing the Service Bus Consumer

This guide explains how to test the Service Bus consumer functions locally and in Azure.

## Local Testing with Azure Service Bus

### Prerequisites

1. Create an Azure Service Bus namespace and queues/topics
2. Get the connection string from Azure Portal

### Setup

1. Update `local.settings.json` with your connection string:
```json
{
  "Values": {
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"
  }
}
```

2. Create the required queues in your Service Bus namespace:
   - `myqueue` (for ServiceBusConsumerFunction)
   - `orders-queue` (for AdvancedServiceBusConsumer)
   - `batch-queue` (for batch processing)

3. Create a topic and subscription (optional):
   - Topic: `mytopic`
   - Subscription: `mysubscription`

### Run the Function App

```bash
func start
```

The output should show:
```
Functions:
    AdvancedServiceBusConsumer: serviceBusTrigger
    ProcessBatchMessages: serviceBusTrigger
    ProcessOrderMessages: serviceBusTrigger
    ServiceBusConsumerFunction: serviceBusTrigger
    ServiceBusTopicConsumerFunction: serviceBusTrigger
```

## Sending Test Messages

### Using Azure Portal

1. Navigate to your Service Bus namespace
2. Select the queue (e.g., `myqueue`)
3. Click "Service Bus Explorer"
4. Click "Send messages"
5. Enter test message body:
```json
{
  "orderId": "ORD-12345",
  "customerId": "CUST-001",
  "amount": 99.99,
  "orderDate": "2025-11-23T10:30:00Z",
  "items": [
    {
      "productId": "PROD-001",
      "productName": "Widget",
      "quantity": 2,
      "price": 49.995
    }
  ]
}
```

### Using Azure CLI

```bash
# Send a simple message
az servicebus queue send \
  --resource-group <resource-group> \
  --namespace-name <namespace> \
  --name myqueue \
  --body "Hello from Azure CLI"

# Send a JSON message
az servicebus queue send \
  --resource-group <resource-group> \
  --namespace-name <namespace> \
  --name orders-queue \
  --body '{"orderId":"ORD-12345","customerId":"CUST-001","amount":99.99,"orderDate":"2025-11-23T10:30:00Z","items":[{"productId":"PROD-001","productName":"Widget","quantity":2,"price":49.995}]}'
```

### Using .NET Producer (See MessageProducer.cs)

You can use the sample producer code to send messages programmatically.

```bash
dotnet run --project MessageProducer
```

## Monitoring Messages

### Local Development

Watch the console output from `func start` to see logs:

```
[2025-11-23T10:30:00.000Z] Executing 'ServiceBusConsumerFunction'
[2025-11-23T10:30:00.001Z] Processing message from Service Bus queue
[2025-11-23T10:30:00.002Z] Message ID: abc123
[2025-11-23T10:30:00.003Z] Message Body: {"orderId":"ORD-12345"...}
[2025-11-23T10:30:00.010Z] Message processed successfully
[2025-11-23T10:30:00.015Z] Executed 'ServiceBusConsumerFunction' (Succeeded)
```

### Azure Application Insights

Once deployed, monitor in Application Insights:

```bash
# View recent traces
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where timestamp > ago(1h) | order by timestamp desc"
```

## Dead Letter Queue

### Access Dead Letter Messages

```bash
# List dead letter messages
az servicebus queue dead-letter list \
  --resource-group <resource-group> \
  --namespace-name <namespace> \
  --name myqueue
```

### Reprocess Dead Letter Messages

Create a function to read from the dead letter queue:

```csharp
[Function("ProcessDeadLetterQueue")]
public void ProcessDeadLetter(
    [ServiceBusTrigger("myqueue/$deadletterqueue", Connection = "ServiceBusConnection")]
    ServiceBusReceivedMessage message)
{
    _logger.LogInformation($"Processing dead letter message: {message.MessageId}");
    _logger.LogInformation($"Dead letter reason: {message.DeadLetterReason}");
    _logger.LogInformation($"Dead letter description: {message.DeadLetterErrorDescription}");
}
```

## Performance Testing

### Load Testing with Apache Bench

First, create a REST endpoint to trigger message sends, then:

```bash
ab -n 1000 -c 10 http://localhost:7071/api/SendMessage
```

### Monitor Performance Metrics

```bash
# Check queue metrics
az monitor metrics list \
  --resource-id "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.ServiceBus/namespaces/<namespace>/queues/myqueue" \
  --metric ActiveMessages,IncomingMessages,OutgoingMessages \
  --start-time 2025-11-23T00:00:00Z \
  --end-time 2025-11-23T23:59:59Z
```

## Troubleshooting

### Message Not Being Received

1. Verify connection string is correct
2. Check queue/topic name matches exactly
3. Ensure function app is running (`func start`)
4. Check if messages are in the queue (Azure Portal)
5. Review function logs for errors

### Messages Going to Dead Letter Queue

1. Check the delivery count in dead letter queue
2. Review `DeadLetterReason` and `DeadLetterErrorDescription`
3. Add try-catch blocks to log detailed error information
4. Verify message format matches expected schema

### Performance Issues

1. Adjust `prefetchCount` in host.json (higher = more throughput)
2. Increase `maxConcurrentCalls` for parallel processing
3. Consider batch processing for high-volume scenarios
4. Monitor CPU and memory usage
5. Check Service Bus namespace throttling limits
