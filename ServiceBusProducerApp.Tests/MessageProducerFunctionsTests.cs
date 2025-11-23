using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceBusProducerApp;
using ServiceBusProducerApp.Services;
using ServiceBusProducerApp.Models;
using System.Text;
using System.Text.Json;

namespace ServiceBusProducerApp.Tests
{
    public class MessageProducerFunctionsTests
    {
        private readonly Mock<ILogger<MessageProducerFunctions>> _mockLogger;
        private readonly Mock<IMessageGenerator> _mockGenerator;
        private readonly Mock<IServiceBusService> _mockServiceBus;
        private readonly MessageProducerFunctions _functions;

        public MessageProducerFunctionsTests()
        {
            _mockLogger = new Mock<ILogger<MessageProducerFunctions>>();
            _mockGenerator = new Mock<IMessageGenerator>();
            _mockServiceBus = new Mock<IServiceBusService>();
            _functions = new MessageProducerFunctions(_mockLogger.Object, _mockGenerator.Object, _mockServiceBus.Object);
        }

        [Fact]
        public async Task GenerateMessages_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new MessageGenerationRequest(
                Count: 5,
                QueueName: "test-queue",
                MinAmount: 0,
                MaxAmount: 100,
                MinItems: 1,
                MaxItems: 10
            );
            var requestJson = JsonSerializer.Serialize(request);
            var httpRequest = CreateHttpRequest(requestJson);

            var classRooms = new List<ClassRoomMessage>
            {
                new("School 1", "District 1", "email@test.com", DateTime.UtcNow, "Pending", new List<ClassRoomItem>())
            };

            _mockGenerator
                .Setup(g => g.GenerateClassRooms(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(classRooms);

            _mockServiceBus
                .Setup(s => s.SendMessagesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _functions.GenerateMessages(httpRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<MessageGenerationResponse>();
        }

        [Fact]
        public async Task GenerateMessages_WithoutQueueOrTopic_ReturnsBadRequest()
        {
            // Arrange
            var request = new MessageGenerationRequest(
                Count: 5,
                QueueName: null,
                TopicName: null
            );
            var requestJson = JsonSerializer.Serialize(request);
            var httpRequest = CreateHttpRequest(requestJson);

            // Act
            var result = await _functions.GenerateMessages(httpRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateMessages_WithInvalidCount_ReturnsBadRequest()
        {
            // Arrange
            var request = new MessageGenerationRequest(
                Count: 2000, // exceeds max of 1000
                QueueName: "test-queue"
            );
            var requestJson = JsonSerializer.Serialize(request);
            var httpRequest = CreateHttpRequest(requestJson);

            // Act
            var result = await _functions.GenerateMessages(httpRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateQueueMessages_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var queueName = "test-queue";
            var count = 10;
            var httpRequest = CreateHttpRequest("");

            var classRooms = Enumerable.Range(1, count)
                .Select(i => new ClassRoomMessage(
                    $"School {i}", 
                    $"District {i}", 
                    $"email{i}@test.com", 
                    DateTime.UtcNow, 
                    "Pending", 
                    new List<ClassRoomItem>()))
                .ToList();

            _mockGenerator
                .Setup(g => g.GenerateClassRooms(count, It.IsAny<int>(), It.IsAny<int>(), 1, 10))
                .Returns(classRooms);

            _mockServiceBus
                .Setup(s => s.SendMessagesAsync(queueName, It.IsAny<List<string>>(), null, false))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _functions.GenerateQueueMessages(httpRequest, queueName, count);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeOfType<MessageGenerationResponse>();
            var response = (MessageGenerationResponse)okResult.Value;
            response.MessagesSent.Should().Be(count);
            response.Destination.Should().Be(queueName);
        }

        [Fact]
        public async Task GenerateTopicMessages_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var topicName = "test-topic";
            var count = 5;
            var httpRequest = CreateHttpRequest("");

            var classRooms = Enumerable.Range(1, count)
                .Select(i => new ClassRoomMessage(
                    $"School {i}", 
                    $"District {i}", 
                    $"email{i}@test.com", 
                    DateTime.UtcNow, 
                    "Pending", 
                    new List<ClassRoomItem>()))
                .ToList();

            _mockGenerator
                .Setup(g => g.GenerateClassRooms(count, It.IsAny<int>(), It.IsAny<int>(), 1, 10))
                .Returns(classRooms);

            _mockServiceBus
                .Setup(s => s.SendMessagesAsync(topicName, It.IsAny<List<string>>(), null, true))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _functions.GenerateTopicMessages(httpRequest, topicName, count);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeOfType<MessageGenerationResponse>();
            var response = (MessageGenerationResponse)okResult.Value;
            response.MessagesSent.Should().Be(count);
            response.Destination.Should().Be(topicName);
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthyStatus()
        {
            // Arrange
            var httpRequest = CreateHttpRequest("");

            // Act
            var result = _functions.HealthCheck(httpRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GenerateMessages_WhenGeneratorThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var request = new MessageGenerationRequest(
                Count: 5,
                QueueName: "test-queue"
            );
            var requestJson = JsonSerializer.Serialize(request);
            var httpRequest = CreateHttpRequest(requestJson);

            _mockGenerator
                .Setup(g => g.GenerateClassRooms(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Throws(new Exception("Generator error"));

            // Act
            var result = await _functions.GenerateMessages(httpRequest);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        private static HttpRequest CreateHttpRequest(string body)
        {
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            return request;
        }
    }
}
