using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ServiceBusConsumerApp.Services;

namespace ServiceBusConsumerApp
{
    public class ServiceBusConsumerFunction
    {
        private readonly ILogger<ServiceBusConsumerFunction> _logger;
        private readonly IEmailService _emailService;

        public ServiceBusConsumerFunction(
            ILogger<ServiceBusConsumerFunction> logger,
            IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [Function("ServiceBusConsumerFunction")]
        public async Task Run(
            [ServiceBusTrigger("myqueue", Connection = "ServiceBusConnection")] 
            string messageBody,
            FunctionContext context)
        {
            _logger.LogInformation("Processing message from Service Bus queue");
            _logger.LogInformation($"Message ID: {context.BindingContext.BindingData["MessageId"]}");
            _logger.LogInformation($"Message Body: {messageBody}");

            try
            {
                // Process the message
                // You can deserialize JSON messages like this:
                // var data = JsonSerializer.Deserialize<YourMessageType>(messageBody);
                
                // Send email notification after processing
                await _emailService.SendEmailAsync(
                    "admin@yourcompany.com",
                    "Message Processed",
                    $"Successfully processed message from queue.\n\nMessage ID: {context.BindingContext.BindingData["MessageId"]}\nContent: {messageBody}"
                );
                
                _logger.LogInformation("Message processed successfully and email sent");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                throw; // This will move the message to dead letter queue if configured
            }
        }

        [Function("ServiceBusTopicConsumerFunction")]
        public async Task RunTopic(
            [ServiceBusTrigger("mytopic", "mysubscription", Connection = "ServiceBusConnection")] 
            string messageBody,
            FunctionContext context)
        {
            _logger.LogInformation("Processing message from Service Bus topic");
            _logger.LogInformation($"Message ID: {context.BindingContext.BindingData["MessageId"]}");
            _logger.LogInformation($"Message Body: {messageBody}");

            try
            {
                // Process the message from topic
                
                // Send email notification
                await _emailService.SendEmailAsync(
                    "admin@yourcompany.com",
                    "Topic Message Processed",
                    $"Successfully processed message from topic.\n\nMessage ID: {context.BindingContext.BindingData["MessageId"]}\nContent: {messageBody}"
                );
                
                _logger.LogInformation("Topic message processed successfully and email sent");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing topic message: {ex.Message}");
                throw;
            }
        }
    }
}
