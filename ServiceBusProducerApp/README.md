# Service Bus Producer App

Azure Function App that generates and sends synthetic messages to Azure Service Bus queues and topics for testing purposes.

## Features

- **Generate synthetic order messages** with realistic random data using Bogus library
- **Configurable message parameters**: count, amount range, item count
- **Support for both queues and topics**
- **Batch message sending** for high performance
- **HTTP API endpoints** for easy integration
- **Azure Key Vault integration** for secure credential management
- **Managed Identity support** for Azure resources

## API Endpoints

### 1. Generate Messages (Generic)
```http
POST /api/messages/generate
Content-Type: application/json

{
  "count": 10,
  "queueName": "orders-queue",
  "minAmount": 50,
  "maxAmount": 500,
  "minItems": 1,
  "maxItems": 5,
  "priority": "High"
}
```

### 2. Generate Queue Messages (Simple)
```http
POST /api/queue/{queueName}/messages?count=10&minAmount=50&maxAmount=500
```

### 3. Generate Topic Messages (Simple)
```http
POST /api/topic/{topicName}/messages?count=10&minAmount=50&maxAmount=500
```

### 4. Health Check
```http
GET /api/health
```

## Message Format

Generated messages follow this structure:

```json
{
  "orderId": "ORD-12345",
  "customerId": "CUST-456",
  "customerEmail": "john.doe@example.com",
  "amount": 245.67,
  "orderDate": "2025-11-23T10:30:00Z",
  "status": "Pending",
  "items": [
    {
      "productId": "PROD-7890",
      "productName": "Ergonomic Steel Bike",
      "quantity": 2,
      "price": 122.84
    }
  ]
}
```

## Configuration

### Local Development

1. Update `local.settings.json`:
```json
{
  "Values": {
    "KeyVault:Uri": "https://your-keyvault.vault.azure.net/",
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;..."
  }
}
```

2. Or use Key Vault (recommended):
```bash
az login
az keyvault secret set --vault-name your-keyvault --name "ServiceBusConnection" --value "your-connection-string"
```

### Azure Deployment

All configuration is stored in Azure Key Vault. No secrets in app settings!

## Setup Instructions

### 1. Create Service Bus Resources

```bash
# Variables
RESOURCE_GROUP="your-resource-group"
NAMESPACE="your-servicebus-namespace"
LOCATION="eastus"

# Create Service Bus namespace
az servicebus namespace create \
  --resource-group $RESOURCE_GROUP \
  --name $NAMESPACE \
  --location $LOCATION \
  --sku Standard

# Create queues for testing
az servicebus queue create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE \
  --name orders-queue

az servicebus queue create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE \
  --name batch-queue

# Create topic and subscription
az servicebus topic create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE \
  --name mytopic

az servicebus topic subscription create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE \
  --topic-name mytopic \
  --name mysubscription
```

### 2. Store Connection String in Key Vault

```bash
# Get Service Bus connection string
CONNECTION_STRING=$(az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv)

# Store in Key Vault
az keyvault secret set \
  --vault-name your-keyvault \
  --name "ServiceBusConnection" \
  --value "$CONNECTION_STRING"
```

### 3. Deploy Function App

```bash
# Create Function App with Managed Identity
FUNCTION_APP="servicebus-producer-app"

az functionapp create \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --storage-account yourstorageaccount \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --assign-identity [system]

# Grant Key Vault access
PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

az keyvault set-policy \
  --name your-keyvault \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Configure app settings
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings "KeyVault:Uri=https://your-keyvault.vault.azure.net/"

# Deploy
func azure functionapp publish $FUNCTION_APP
```

## Usage Examples

### Using cURL

```bash
# Generate 50 messages to a queue
curl -X POST "http://localhost:7071/api/queue/orders-queue/messages?count=50&minAmount=100&maxAmount=1000" \
  -H "Content-Type: application/json"

# Generate custom messages
curl -X POST "http://localhost:7071/api/messages/generate" \
  -H "Content-Type: application/json" \
  -d '{
    "count": 100,
    "queueName": "orders-queue",
    "minAmount": 10,
    "maxAmount": 5000,
    "minItems": 1,
    "maxItems": 20,
    "priority": "High"
  }'
```

### Using PowerShell

```powershell
# Generate messages
$body = @{
    count = 100
    queueName = "orders-queue"
    minAmount = 50
    maxAmount = 1000
    minItems = 1
    maxItems = 10
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/messages/generate" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

### Load Testing

Generate high volume of messages for load testing:

```bash
# Generate 1000 messages
for i in {1..10}; do
  curl -X POST "http://localhost:7071/api/queue/batch-queue/messages?count=100" &
done
wait
```

## Message Generation Details

### Bogus Library

The app uses the [Bogus](https://github.com/bchavez/Bogus) library to generate realistic fake data:

- **Order IDs**: ORD-10000 to ORD-99999
- **Customer IDs**: CUST-100 to CUST-999
- **Customer Emails**: Realistic email addresses
- **Product Names**: Commerce product names
- **Dates**: Random dates within the last 30 days
- **Status**: Random order statuses

### Customization

You can customize the data generation by modifying `MessageGenerator.cs`:

```csharp
_orderFaker = new Faker<OrderMessage>()
    .RuleFor(o => o.OrderId, f => $"ORD-{f.Random.Number(10000, 99999)}")
    .RuleFor(o => o.CustomerId, f => $"CUST-{f.Random.Number(100, 999)}")
    // Add your custom rules here
```

## Running Locally

```bash
cd ServiceBusProducerApp
func start
```

The function app will be available at `http://localhost:7071`

## Monitoring

View function execution logs:

```bash
# In Azure
func azure functionapp logstream $FUNCTION_APP

# Local development
# Logs appear in the console where func start is running
```

## Troubleshooting

### "ServiceBusConnection configuration is missing"
- Ensure Key Vault URI is configured
- Verify the secret exists in Key Vault
- Check Managed Identity has access to Key Vault

### "Message is too large"
- Reduce the number of items per order
- Service Bus has a 256 KB message size limit

### Rate limiting
- Service Bus has throughput limits based on SKU
- Consider using Premium tier for high throughput scenarios

## Security

✅ **Best Practices Implemented:**
- Secrets stored in Azure Key Vault
- Managed Identity for authentication
- No credentials in code or configuration files
- Function-level authorization for API endpoints

## Performance

- Batch sending for optimal throughput
- Configurable batch sizes
- Async/await for non-blocking operations
- Efficient message serialization with System.Text.Json

## Related Projects

- [ServiceBusConsumerApp](../ServiceBusConsumerApp) - Consumes and processes these messages
