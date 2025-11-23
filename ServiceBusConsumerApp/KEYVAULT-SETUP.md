# Azure Key Vault Integration

This application uses Azure Key Vault with **Managed Identity** to securely store and access secrets like connection strings and API keys.

## Architecture

```
Azure Function App (Managed Identity)
         ↓
Azure Key Vault
         ↓
Secrets: ServiceBusConnection, SendGridApiKey
```

## Required Key Vault Secrets

Store the following secrets in your Azure Key Vault:

| Secret Name | Description | Example Value |
|------------|-------------|---------------|
| `ServiceBusConnection` | Service Bus connection string (full or namespace only) | `Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...` or `your-namespace.servicebus.windows.net` |
| `SendGridApiKey` | SendGrid API Key | `SG.xxxxxxxxxxxxx` |
| `SendGridFromEmail` | SendGrid sender email address | `noreply@yourcompany.com` |
| `SendGridFromName` | SendGrid sender display name | `Your Company Name` |

**Note:** For Service Bus, you can store either:
- Full connection string (with access keys) - traditional approach
- Just the namespace (e.g., `your-namespace.servicebus.windows.net`) - for Managed Identity authentication (recommended)

## Setup Instructions

### 1. Create Azure Key Vault

```bash
# Variables
RESOURCE_GROUP="your-resource-group"
KEY_VAULT_NAME="your-keyvault-name"
LOCATION="eastus"

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization false
```

### 2. Add Secrets to Key Vault

```bash
# Option 1: Add Service Bus connection string (traditional with access keys)
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ServiceBusConnection" \
  --value "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"

# Option 2: Add Service Bus namespace only (recommended - uses Managed Identity)
# First, grant the Function App Managed Identity access to Service Bus:
az role assignment create \
  --role "Azure Service Bus Data Receiver" \
  --assignee-object-id $PRINCIPAL_ID \
  --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.ServiceBus/namespaces/your-servicebus-namespace"

# Then store just the namespace:
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ServiceBusConnection__fullyQualifiedNamespace" \
  --value "your-namespace.servicebus.windows.net"

# Add SendGrid API Key
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "SendGridApiKey" \
  --value "SG.your-sendgrid-api-key"

# Add SendGrid sender email
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "SendGridFromEmail" \
  --value "noreply@yourcompany.com"

# Add SendGrid sender name
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "SendGridFromName" \
  --value "Your Company Name"
```

### 3. Create Azure Function App with Managed Identity

```bash
FUNCTION_APP_NAME="your-function-app"
STORAGE_ACCOUNT="yourstorageaccount"

# Create Function App with System-Assigned Managed Identity
az functionapp create \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_ACCOUNT \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --assign-identity [system]
```

### 4. Grant Function App Access to Key Vault

```bash
# Get Function App's Managed Identity Principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# Grant "Key Vault Secrets User" role to the Function App
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Or use RBAC (if RBAC is enabled on Key Vault)
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee-object-id $PRINCIPAL_ID \
  --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME"
```

### 5. Configure Function App Settings

```bash
# Set Key Vault URI in Function App configuration
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings "KeyVault:Uri=https://$KEY_VAULT_NAME.vault.azure.net/"

# All SendGrid configuration is now stored in Key Vault
# No additional app settings needed for SendGrid
```

### 6. Deploy Function App

```bash
func azure functionapp publish $FUNCTION_APP_NAME
```

## Local Development

For local development, authenticate using Azure CLI:

```bash
# Login to Azure
az login

# Set the subscription
az account set --subscription "your-subscription-id"
```

Then update `local.settings.json`:

```json
{
  "Values": {
    "KeyVault:Uri": "https://your-keyvault-name.vault.azure.net/"
  }
}
```

The `DefaultAzureCredential` will automatically use your Azure CLI credentials locally.

### Alternative: Local Development Without Key Vault

For local development without Key Vault access, you can add secrets directly to `local.settings.json`:

```json
{
  "Values": {
    "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...",
    "SendGridApiKey": "SG.your-key",
    "SendGridFromEmail": "noreply@yourcompany.com",
    "SendGridFromName": "Your Company Name"
  }
}
```

Or for Managed Identity approach:

```json
{
  "Values": {
    "ServiceBusConnection__fullyQualifiedNamespace": "your-namespace.servicebus.windows.net",
    "SendGridApiKey": "SG.your-key",
    "SendGridFromEmail": "noreply@yourcompany.com",
    "SendGridFromName": "Your Company Name"
  }
}
```

⚠️ **Never commit secrets to source control!**

## How It Works

### DefaultAzureCredential Authentication Chain

The application uses `DefaultAzureCredential` which tries authentication methods in this order:

1. **Environment Variables** (`AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`)
2. **Managed Identity** (when deployed to Azure)
3. **Visual Studio** (local development)
4. **Azure CLI** (local development)
5. **Azure PowerShell** (local development)

### Service Bus with Managed Identity

The application supports two authentication methods for Service Bus:

**Option 1: Traditional Connection String (stored in Key Vault)**
```
ServiceBusConnection=Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...
```

**Option 2: Managed Identity (recommended - stored in Key Vault)**
```
ServiceBusConnection__fullyQualifiedNamespace=your-namespace.servicebus.windows.net
```

With Option 2, the Function App's Managed Identity authenticates to Service Bus automatically - no access keys needed!

## Security Best Practices

✅ **Implemented:**
- Secrets stored in Azure Key Vault (not in code or config files)
- Managed Identity for authentication (no credentials in code)
- RBAC for least-privilege access
- Automatic credential rotation support

✅ **Recommended:**
- Enable Key Vault soft delete and purge protection
- Enable Key Vault diagnostic logging
- Use separate Key Vaults for dev/staging/production
- Implement Key Vault firewall rules
- Monitor Key Vault access with Azure Monitor

## Troubleshooting

### "Azure.Identity.AuthenticationFailedException"

**Problem:** Function can't authenticate to Key Vault.

**Solutions:**
1. Verify Managed Identity is enabled on the Function App
2. Check Key Vault access policies include the Function App's identity
3. For local dev, run `az login` to authenticate

### "SecretNotFound"

**Problem:** Secret doesn't exist in Key Vault.

**Solutions:**
1. Verify secret name matches exactly (case-sensitive)
2. Check secret exists: `az keyvault secret show --vault-name $KEY_VAULT_NAME --name ServiceBusConnection`
3. Ensure Key Vault URI is correct in configuration

### Service Bus Authentication

**Managed Identity (Recommended):**
Store in Key Vault as `ServiceBusConnection__fullyQualifiedNamespace`:
```
your-namespace.servicebus.windows.net
```

**Traditional Connection String:**
Store in Key Vault as `ServiceBusConnection`:
```
Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...
```

For Managed Identity, ensure the Function App has the **Azure Service Bus Data Receiver** role on the Service Bus namespace.

## Monitoring

View Key Vault access logs:

```bash
az monitor diagnostic-settings create \
  --name KeyVaultDiagnostics \
  --resource "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME" \
  --logs '[{"category": "AuditEvent","enabled": true}]' \
  --workspace "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.OperationalInsights/workspaces/your-workspace"
```

## References

- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [DefaultAzureCredential](https://docs.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
