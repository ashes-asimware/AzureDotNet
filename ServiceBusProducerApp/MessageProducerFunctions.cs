using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ServiceBusProducerApp.Models;
using ServiceBusProducerApp.Services;

namespace ServiceBusProducerApp
{
    public class MessageProducerFunctions
    {
        private readonly ILogger<MessageProducerFunctions> _logger;
        private readonly IMessageGenerator _messageGenerator;
        private readonly IServiceBusService _serviceBusService;

        public MessageProducerFunctions(
            ILogger<MessageProducerFunctions> logger,
            IMessageGenerator messageGenerator,
            IServiceBusService serviceBusService)
        {
            _logger = logger;
            _messageGenerator = messageGenerator;
            _serviceBusService = serviceBusService;
        }

        [Function("GenerateMessages")]
        public async Task<IActionResult> GenerateMessages(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "messages/generate")] HttpRequest httpReq)
        {
            try
            {
                // Parse request body
                var requestBody = await new StreamReader(httpReq.Body).ReadToEndAsync();
                var request = JsonSerializer.Deserialize<MessageGenerationRequest?>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (!request.HasValue)
                {
                    return new BadRequestObjectResult("Invalid request body");
                }
                
                var req = request.Value;

                // Validate request
                if (string.IsNullOrEmpty(req.QueueName) && string.IsNullOrEmpty(req.TopicName))
                {
                    return new BadRequestObjectResult("Either QueueName or TopicName must be specified");
                }

                if (req.Count < 1 || req.Count > 1000)
                {
                    return new BadRequestObjectResult("Count must be between 1 and 1000");
                }

                _logger.LogInformation($"Generating {req.Count} synthetic messages");

                // Generate messages (convert amount ranges to score ranges)
                var classRooms = _messageGenerator.GenerateClassRooms(
                    req.Count,
                    (int)req.MinAmount,  // MinScore
                    (int)req.MaxAmount,  // MaxScore
                    req.MinItems,
                    req.MaxItems
                );

                // Serialize messages
                var messageContents = classRooms.Select(o => JsonSerializer.Serialize(o)).ToList();

                // Prepare application properties
                var appProperties = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(req.Priority))
                {
                    appProperties["Priority"] = req.Priority;
                }
                
                // Add school email for each message (from the classroom data)
                // Note: Since we're batching, we'll add generic properties here
                // For per-message properties, we'd need to modify the service

                // Send to Service Bus
                string destination = !string.IsNullOrEmpty(req.QueueName) ? req.QueueName : req.TopicName!;
                bool isTopic = !string.IsNullOrEmpty(req.TopicName);

                await _serviceBusService.SendMessagesAsync(destination, messageContents, appProperties, isTopic);

                var response = new MessageGenerationResponse(
                    MessagesSent: req.Count,
                    Destination: destination,
                    MessageIds: classRooms.Select(o => o.School).ToList()
                );

                _logger.LogInformation($"Successfully sent {req.Count} messages to {destination}");

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating messages: {ex.Message}");
                return new ObjectResult(new MessageGenerationResponse(
                    MessagesSent: 0,
                    Destination: string.Empty,
                    MessageIds: new List<string>(),
                    Status: "Error",
                    ErrorMessage: ex.Message
                ))
                {
                    StatusCode = 500
                };
            }
        }

        [Function("GenerateQueueMessages")]
        public async Task<IActionResult> GenerateQueueMessages(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "queue/{queueName}/messages")] HttpRequest req,
            string queueName,
            [FromQuery] int count = 1,
            [FromQuery] decimal minAmount = 10,
            [FromQuery] decimal maxAmount = 1000)
        {
            try
            {
                _logger.LogInformation($"Generating {count} messages for queue: {queueName}");

                var classRooms = _messageGenerator.GenerateClassRooms(count, (int)minAmount, (int)maxAmount, 1, 10);
                var messageContents = classRooms.Select(o => JsonSerializer.Serialize(o)).ToList();

                await _serviceBusService.SendMessagesAsync(queueName, messageContents);

                return new OkObjectResult(new MessageGenerationResponse(
                    MessagesSent: count,
                    Destination: queueName,
                    MessageIds: classRooms.Select(o => o.School).ToList()
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating queue messages: {ex.Message}");
                return new ObjectResult(new MessageGenerationResponse(
                    MessagesSent: 0,
                    Destination: string.Empty,
                    MessageIds: new List<string>(),
                    Status: "Error",
                    ErrorMessage: ex.Message
                ))
                {
                    StatusCode = 500
                };
            }
        }

        [Function("GenerateTopicMessages")]
        public async Task<IActionResult> GenerateTopicMessages(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "topic/{topicName}/messages")] HttpRequest req,
            string topicName,
            [FromQuery] int count = 1,
            [FromQuery] decimal minAmount = 10,
            [FromQuery] decimal maxAmount = 1000)
        {
            try
            {
                _logger.LogInformation($"Generating {count} messages for topic: {topicName}");

                var classRooms = _messageGenerator.GenerateClassRooms(count, (int)minAmount, (int)maxAmount, 1, 10);
                var messageContents = classRooms.Select(o => JsonSerializer.Serialize(o)).ToList();

                await _serviceBusService.SendMessagesAsync(topicName, messageContents, null, true);

                return new OkObjectResult(new MessageGenerationResponse(
                    MessagesSent: count,
                    Destination: topicName,
                    MessageIds: classRooms.Select(o => o.School).ToList()
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating topic messages: {ex.Message}");
                return new ObjectResult(new MessageGenerationResponse(
                    MessagesSent: 0,
                    Destination: string.Empty,
                    MessageIds: new List<string>(),
                    Status: "Error",
                    ErrorMessage: ex.Message
                ))
                {
                    StatusCode = 500
                };
            }
        }

        [Function("HealthCheck")]
        public IActionResult HealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        {
            return new OkObjectResult(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "ServiceBusProducerApp"
            });
        }
    }
}
