# AzureDotNet.Shared.Configuration

Shared NuGet package for reading configuration values from Azure Key Vault with Managed Identity support.

## Features

- 🔐 **Azure Key Vault integration** with DefaultAzureCredential (Managed Identity)
- 🎯 **Strongly-typed configuration** access via IAzureConfigurationProvider
- 🔄 **Reusable across projects** - eliminate duplicate Key Vault code
- ⚡ **Easy setup** - one-line extension methods
- 🛡️ **Safe defaults** - graceful handling when Key Vault is not configured

## Installation

```bash
dotnet add package AzureDotNet.Shared.Configuration
```

Or reference the local project:

```xml
<ItemGroup>
  <ProjectReference Include="../AzureDotNet.Shared.Configuration/AzureDotNet.Shared.Configuration.csproj" />
</ItemGroup>
```

## Usage

### 1. Configure Key Vault in Program.cs

```csharp
using AzureDotNet.Shared.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

// Add Key Vault configuration (reads from "KeyVault:Uri" setting)
builder.Configuration.AddAzureKeyVaultWithManagedIdentity();

// Register configuration provider
builder.Services.AddAzureConfiguration();

builder.Build().Run();
```

### 2. Inject and Use Configuration

```csharp
public class MyService
{
    private readonly IAzureConfigurationProvider _config;

    public MyService(IAzureConfigurationProvider config)
    {
        _config = config;
    }

    public void DoWork()
    {
        // Access strongly-typed properties
        var serviceBusConnection = _config.ServiceBusConnection;
        var sendGridApiKey = _config.SendGridApiKey;
        var fromEmail = _config.SendGridFromEmail;
        var fromName = _config.SendGridFromName;

        // Or get custom secrets
        var customSecret = _config.GetSecret("MyCustomSecret");
        var optionalSecret = _config.GetSecretOrDefault("OptionalSecret", "default-value");
    }
}
```

## Configuration Keys

The package reads the following configuration keys:

| Key | Description | Required |
|-----|-------------|----------|
| `KeyVault:Uri` | Azure Key Vault URI | No (optional for local dev) |
| `ServiceBusConnection` | Service Bus connection string | Yes |
| `ServiceBusConnection__fullyQualifiedNamespace` | Service Bus namespace for Managed Identity | Alternative to above |
| `SendGridApiKey` | SendGrid API key | Yes |
| `SendGridFromEmail` | SendGrid sender email | Yes |
| `SendGridFromName` | SendGrid sender name | Yes |

## API Reference

### Extension Methods

#### `AddAzureKeyVaultWithManagedIdentity()`

Configures Azure Key Vault as a configuration source.

```csharp
builder.Configuration.AddAzureKeyVaultWithManagedIdentity();

// Or specify a custom URI key
builder.Configuration.AddAzureKeyVaultWithManagedIdentity("MyApp:KeyVaultUri");

// Or pass URI directly
builder.Configuration.AddAzureKeyVaultWithManagedIdentity(
    new Uri("https://mykeyvault.vault.azure.net/"));
```

#### `AddAzureConfiguration()`

Registers `IAzureConfigurationProvider` with dependency injection.

```csharp
builder.Services.AddAzureConfiguration();
```

### IAzureConfigurationProvider

Interface for accessing configuration values.

**Properties:**
- `string ServiceBusConnection` - Service Bus connection string or namespace
- `string SendGridApiKey` - SendGrid API key
- `string SendGridFromEmail` - SendGrid sender email
- `string SendGridFromName` - SendGrid sender display name

**Methods:**
- `string GetSecret(string key)` - Gets a secret by key (throws if not found)
- `string? GetSecretOrDefault(string key, string? defaultValue = null)` - Gets a secret or returns default

## Authentication

The package uses `DefaultAzureCredential` which tries authentication methods in this order:

1. **Environment Variables** - `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`
2. **Managed Identity** - When deployed to Azure (Function App, App Service, etc.)
3. **Visual Studio** - Local development with VS
4. **Azure CLI** - Local development with `az login`
5. **Azure PowerShell** - Local development with PowerShell

## Local Development

For local development, authenticate using Azure CLI:

```bash
az login
az account set --subscription "your-subscription-id"
```

Then set the Key Vault URI in `local.settings.json`:

```json
{
  "Values": {
    "KeyVault:Uri": "https://your-keyvault.vault.azure.net/"
  }
}
```

Alternatively, add secrets directly to `local.settings.json` without Key Vault:

```json
{
  "Values": {
    "ServiceBusConnection": "Endpoint=sb://...",
    "SendGridApiKey": "SG.your-key",
    "SendGridFromEmail": "noreply@example.com",
    "SendGridFromName": "Your Company"
  }
}
```

## Example: Azure Function

```csharp
using AzureDotNet.Shared.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure Key Vault
builder.Configuration.AddAzureKeyVaultWithManagedIdentity();

// Register configuration provider
builder.Services.AddAzureConfiguration();

builder.Build().Run();
```

## Example: Using in a Service

```csharp
using AzureDotNet.Shared.Configuration;

public class EmailService
{
    private readonly ISendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(
        IAzureConfigurationProvider config,
        ILogger<EmailService> logger)
    {
        // Get SendGrid configuration from Key Vault
        var apiKey = config.SendGridApiKey;
        _fromEmail = config.SendGridFromEmail;
        _fromName = config.SendGridFromName;

        _client = new SendGridClient(apiKey);
    }
}
```

## Benefits

✅ **Single source of truth** - One place to manage Key Vault integration  
✅ **No duplicate code** - Reuse across multiple projects  
✅ **Type-safe** - Strongly-typed properties with validation  
✅ **Easy testing** - Mock IAzureConfigurationProvider in tests  
✅ **Flexible** - Works with or without Key Vault  
✅ **Secure** - Uses Managed Identity, no credentials in code

## Error Handling

The provider throws `InvalidOperationException` with descriptive messages when required configuration is missing:

```csharp
try
{
    var connection = config.ServiceBusConnection;
}
catch (InvalidOperationException ex)
{
    // ex.Message: "ServiceBusConnection configuration is missing. 
    //              Ensure either 'ServiceBusConnection' or 
    //              'ServiceBusConnection__fullyQualifiedNamespace' 
    //              is configured in Key Vault or app settings."
}
```

## Building and Packaging

```bash
cd AzureDotNet.Shared.Configuration

# Build
dotnet build

# Pack as NuGet package
dotnet pack -c Release

# Publish to local feed
dotnet nuget push bin/Release/*.nupkg --source local-feed
```

## License

MIT
