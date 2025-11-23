using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using AzureDotNet.Shared.Configuration;

namespace ServiceBusProducerApp.Services
{
    public interface IServiceBusService
    {
        Task SendMessagesAsync(string destination, List<string> messages, Dictionary<string, object>? applicationProperties = null, bool isTopic = false);
    }

    public class ServiceBusService : IServiceBusService
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusService> _logger;

        public ServiceBusService(IAzureConfigurationProvider configuration, ILogger<ServiceBusService> logger)
        {
            _logger = logger;
            
            var connectionString = configuration.ServiceBusConnection;

            // Check if it's a connection string or a fully qualified namespace
            if (connectionString.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                // Traditional connection string
                _client = new ServiceBusClient(connectionString);
            }
            else
            {
                // Fully qualified namespace - use Managed Identity
                var credential = new DefaultAzureCredential();
                _client = new ServiceBusClient(connectionString, credential);
            }
        }

        public async Task SendMessagesAsync(
            string destination, 
            List<string> messages, 
            Dictionary<string, object>? applicationProperties = null, 
            bool isTopic = false)
        {
            ServiceBusSender sender = _client.CreateSender(destination);

            try
            {
                // Create a batch
                using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

                foreach (var messageContent in messages)
                {
                    var message = new ServiceBusMessage(messageContent)
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString()
                    };

                    // Add application properties if provided
                    if (applicationProperties != null)
                    {
                        foreach (var prop in applicationProperties)
                        {
                            message.ApplicationProperties[prop.Key] = prop.Value;
                        }
                    }

                    if (!messageBatch.TryAddMessage(message))
                    {
                        // If the message doesn't fit in the batch, send current batch and create a new one
                        await sender.SendMessagesAsync(messageBatch);
                        _logger.LogInformation($"Sent batch of messages to {destination}");

                        // Create new batch and add the message
                        using ServiceBusMessageBatch newBatch = await sender.CreateMessageBatchAsync();
                        if (!newBatch.TryAddMessage(message))
                        {
                            throw new Exception($"Message is too large to fit in a batch");
                        }
                    }
                }

                // Send the final batch
                if (messageBatch.Count > 0)
                {
                    await sender.SendMessagesAsync(messageBatch);
                    _logger.LogInformation($"Sent batch of {messageBatch.Count} message(s) to {destination}");
                }
            }
            finally
            {
                await sender.DisposeAsync();
            }
        }
    }
}
