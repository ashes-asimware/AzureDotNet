using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using ServiceBusConsumerApp;
using ServiceBusConsumerApp.Services;
using ServiceBusConsumerApp.Models;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;

namespace ServiceBusConsumerApp.Tests
{
    public class AdvancedServiceBusConsumerTests
    {
        private readonly Mock<ILogger<AdvancedServiceBusConsumer>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AdvancedServiceBusConsumer _consumer;
        private readonly Mock<FunctionContext> _mockContext;

        public AdvancedServiceBusConsumerTests()
        {
            _mockLogger = new Mock<ILogger<AdvancedServiceBusConsumer>>();
            _mockEmailService = new Mock<IEmailService>();
            _consumer = new AdvancedServiceBusConsumer(_mockLogger.Object, _mockEmailService.Object);
            _mockContext = new Mock<FunctionContext>();
        }

        [Fact]
        public async Task ProcessClassRoom_WithValidMessage_ProcessesSuccessfully()
        {
            // Arrange
            var classRoomMessage = new ClassRoomMessage(
                School: "Test School",
                District: "Test District",
                SchoolEmail: "test@school.com",
                ReportedOn: DateTime.UtcNow,
                Status: "Active",
                Items: new List<ClassRoomItem>
                {
                    new ClassRoomItem("John Doe", 1001, "Ms. Smith", 201, "Math", 101, 95),
                    new ClassRoomItem("Jane Smith", 1002, "Mr. Johnson", 202, "Science", 102, 88)
                }
            );

            var messageBody = JsonSerializer.Serialize(classRoomMessage);
            var messageBytes = Encoding.UTF8.GetBytes(messageBody);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBytes),
                messageId: "test-message-id",
                contentType: "application/json"
            );

            _mockEmailService
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            // Act
            await _consumer.ProcessClassRoom(message, _mockContext.Object);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Classroom report processed successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessClassRoom_WithInvalidJson_LogsWarning()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            var messageBytes = Encoding.UTF8.GetBytes(invalidJson);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBytes),
                messageId: "test-message-id"
            );

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(
                () => _consumer.ProcessClassRoom(message, _mockContext.Object));
        }

        [Fact]
        public async Task ProcessBatch_WithMultipleMessages_ProcessesAll()
        {
            // Arrange
            var messages = new List<ServiceBusReceivedMessage>();
            for (int i = 0; i < 3; i++)
            {
                var classRoom = new ClassRoomMessage(
                    School: $"School {i}",
                    District: $"District {i}",
                    SchoolEmail: $"school{i}@test.com",
                    ReportedOn: DateTime.UtcNow,
                    Status: "Active",
                    Items: new List<ClassRoomItem>()
                );

                var messageBody = JsonSerializer.Serialize(classRoom);
                var messageBytes = Encoding.UTF8.GetBytes(messageBody);
                messages.Add(ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData(messageBytes),
                    messageId: $"msg-{i}"
                ));
            }

            _mockEmailService
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            // Act
            await _consumer.ProcessBatch(messages.ToArray(), _mockContext.Object);

            // Assert
            _mockEmailService.Verify(
                e => e.SendEmailAsync(
                    "admin@yourcompany.com",
                    "Batch Processing Complete",
                    It.Is<string>(s => s.Contains("Successful: 3")),
                    null),
                Times.Once);
        }

        [Fact]
        public async Task ProcessBatch_WithSomeFailures_TracksFailedCount()
        {
            // Arrange
            var validMessage = new ClassRoomMessage(
                School: "Valid School",
                District: "Valid District",
                SchoolEmail: "valid@school.com",
                ReportedOn: DateTime.UtcNow,
                Status: "Active",
                Items: new List<ClassRoomItem>()
            );
            var validMessageBody = JsonSerializer.Serialize(validMessage);
            var validBytes = Encoding.UTF8.GetBytes(validMessageBody);

            var messages = new[]
            {
                ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData(validBytes),
                    messageId: "valid-msg"),
                ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData(Encoding.UTF8.GetBytes("invalid json")),
                    messageId: "invalid-msg")
            };

            _mockEmailService
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            // Act
            await _consumer.ProcessBatch(messages, _mockContext.Object);

            // Assert
            _mockEmailService.Verify(
                e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<string>(s => s.Contains("Successful: 1") && s.Contains("Failed: 1")),
                    null),
                Times.Once);
        }
    }
}
