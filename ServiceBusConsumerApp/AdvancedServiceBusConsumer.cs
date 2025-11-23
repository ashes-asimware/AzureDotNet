using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceBusConsumerApp.Models;
using ServiceBusConsumerApp.Services;
using System.Text.Json;

namespace ServiceBusConsumerApp
{
    /// <summary>
    /// Advanced Service Bus consumer demonstrating typed message handling
    /// and access to message properties.
    /// </summary>
    public class AdvancedServiceBusConsumer
    {
        private readonly ILogger<AdvancedServiceBusConsumer> _logger;
        private readonly IEmailService _emailService;

        public AdvancedServiceBusConsumer(
            ILogger<AdvancedServiceBusConsumer> logger,
            IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [Function("ProcessClassRoomMessages")]
        public async Task ProcessClassRoom(
            [ServiceBusTrigger("classroom-queue", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            FunctionContext context)
        {
            _logger.LogInformation("Processing classroom message");
            _logger.LogInformation($"Message ID: {message.MessageId}");
            _logger.LogInformation($"Correlation ID: {message.CorrelationId}");
            _logger.LogInformation($"Content Type: {message.ContentType}");
            _logger.LogInformation($"Delivery Count: {message.DeliveryCount}");
            _logger.LogInformation($"Enqueued Time: {message.EnqueuedTime}");
            _logger.LogInformation($"Sequence Number: {message.SequenceNumber}");

            try
            {
                // Deserialize the message body
                var classRoomMessage = JsonSerializer.Deserialize<ClassRoomMessage>(message.Body.ToString());
                
                if (classRoomMessage is null)
                {
                    _logger.LogWarning("Failed to deserialize classroom message");
                    return;
                }

                _logger.LogInformation($"School: {classRoomMessage.School}");
                _logger.LogInformation($"District: {classRoomMessage.District}");
                _logger.LogInformation($"Status: {classRoomMessage.Status}");
                _logger.LogInformation($"Reported On: {classRoomMessage.ReportedOn}");
                _logger.LogInformation($"Items Count: {classRoomMessage.Items.Count}");

                // Access custom application properties
                if (message.ApplicationProperties.ContainsKey("Priority"))
                {
                    _logger.LogInformation($"Priority: {message.ApplicationProperties["Priority"]}");
                }

                // Process the classroom report
                ProcessClassRoomLogic(classRoomMessage);

                // Send report confirmation email if school email is available
                if (message.ApplicationProperties.ContainsKey("SchoolEmail"))
                {
                    var schoolEmail = message.ApplicationProperties["SchoolEmail"].ToString();
                    await _emailService.SendEmailAsync(
                        schoolEmail ?? "school@example.com",
                        "Classroom Report Received",
                        $"Your classroom report for {classRoomMessage.School} has been received and processed.\n\nReported On: {classRoomMessage.ReportedOn}\nStudents: {classRoomMessage.Items.Count}"
                    );
                    _logger.LogInformation($"Report confirmation email sent to {schoolEmail}");
                }

                _logger.LogInformation("Classroom report processed successfully");
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Failed to deserialize message: {ex.Message}");
                throw; // Will send to dead letter queue
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order: {ex.Message}");
                throw;
            }
        }

        [Function("ProcessBatchMessages")]
        public async Task ProcessBatch(
            [ServiceBusTrigger("batch-queue", Connection = "ServiceBusConnection", IsBatched = true)]
            ServiceBusReceivedMessage[] messages,
            FunctionContext context)
        {
            _logger.LogInformation($"Processing batch of {messages.Length} messages");

            var processedCount = 0;
            var failedCount = 0;

            foreach (var message in messages)
            {
                try
                {
                    _logger.LogInformation($"Processing message: {message.MessageId}");
                    
                    var classRoomMessage = JsonSerializer.Deserialize<ClassRoomMessage>(message.Body.ToString());
                    
                    if (classRoomMessage is not null)
                    {
                        ProcessClassRoomLogic(classRoomMessage);
                        processedCount++;
                        _logger.LogInformation($"Message {message.MessageId} processed successfully");
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError($"Error processing message {message.MessageId}: {ex.Message}");
                    // Note: In batch mode, you need to handle errors carefully
                    // Individual message failures won't automatically move to DLQ
                }
            }

            // Send batch summary email
            await _emailService.SendEmailAsync(
                "admin@yourcompany.com",
                "Batch Processing Complete",
                $"Batch processing completed.\n\nTotal messages: {messages.Length}\nSuccessful: {processedCount}\nFailed: {failedCount}"
            );

            _logger.LogInformation("Batch processing completed");
        }

        private void ProcessClassRoomLogic(ClassRoomMessage classRoom)
        {
            // Add your business logic here
            _logger.LogInformation($"Processing classroom report for {classRoom.School} with {classRoom.Items.Count} students");
            
            // Example: Calculate average score
            if (classRoom.Items.Any())
            {
                var averageScore = classRoom.Items.Average(item => item.Score);
                _logger.LogInformation($"Average score: {averageScore:F2}");
            }

            // Example: Find top performers
            var topPerformers = classRoom.Items
                .OrderByDescending(item => item.Score)
                .Take(5)
                .Select(item => $"{item.StudentName} ({item.Score})")
                .ToList();
            
            if (topPerformers.Any())
            {
                _logger.LogInformation($"Top performers: {string.Join(", ", topPerformers)}");
            }
        }
    }
}
