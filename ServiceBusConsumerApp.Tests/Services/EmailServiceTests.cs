using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using ServiceBusConsumerApp.Services;
using AzureDotNet.Shared.Configuration;

namespace ServiceBusConsumerApp.Tests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<ISendGridClient> _mockSendGridClient;
        private readonly Mock<IAzureConfigurationProvider> _mockConfig;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            _mockSendGridClient = new Mock<ISendGridClient>();
            _mockConfig = new Mock<IAzureConfigurationProvider>();
            _mockLogger = new Mock<ILogger<EmailService>>();

            _mockConfig.Setup(c => c.SendGridFromEmail).Returns("test@example.com");
            _mockConfig.Setup(c => c.SendGridFromName).Returns("Test Sender");

            _emailService = new EmailService(_mockSendGridClient.Object, _mockConfig.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendEmailAsync_WithValidParameters_SendsEmail()
        {
            // Arrange
            var toEmail = "recipient@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Create a mock Response that returns success status
            var response = new Response(System.Net.HttpStatusCode.OK, null, null);
            
            _mockSendGridClient
                .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            await _emailService.SendEmailAsync(toEmail, subject, body);

            // Assert - just verify SendEmailAsync was called once
            _mockSendGridClient.Verify(
                c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_WithHtmlContent_SendsHtmlEmail()
        {
            // Arrange
            var toEmail = "recipient@example.com";
            var subject = "Test Subject";
            var body = "Plain text";
            var htmlContent = "<html><body>HTML content</body></html>";

            var response = new Response(System.Net.HttpStatusCode.OK, null, null);
            
            _mockSendGridClient
                .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            await _emailService.SendEmailAsync(toEmail, subject, body, htmlContent);

            // Assert - just verify SendEmailAsync was called once
            _mockSendGridClient.Verify(
                c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendOrderConfirmationAsync_WithValidOrderData_SendsConfirmation()
        {
            // Arrange
            var toEmail = "customer@example.com";
            var orderId = "ORD-12345";
            var amount = 99.99m;

            var response = new Response(System.Net.HttpStatusCode.OK, null, null);
            
            _mockSendGridClient
                .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            await _emailService.SendOrderConfirmationAsync(toEmail, orderId, amount);

            // Assert - just verify SendEmailAsync was called once
            _mockSendGridClient.Verify(
                c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_WhenSendGridFails_ThrowsException()
        {
            // Arrange
            var toEmail = "recipient@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            _mockSendGridClient
                .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SendGrid error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _emailService.SendEmailAsync(toEmail, subject, body));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task SendEmailAsync_WithInvalidEmail_ThrowsException(string? invalidEmail)
        {
            // Arrange
            var subject = "Test Subject";
            var body = "Test Body";

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(
                () => _emailService.SendEmailAsync(invalidEmail!, subject, body));
        }
    }
}
