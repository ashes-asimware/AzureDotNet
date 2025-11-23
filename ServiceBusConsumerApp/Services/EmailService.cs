using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Logging;
using AzureDotNet.Shared.Configuration;

namespace ServiceBusConsumerApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, string? htmlContent = null);
        Task SendOrderConfirmationAsync(string toEmail, string orderId, decimal amount);
    }

    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(
            ISendGridClient sendGridClient,
            IAzureConfigurationProvider configuration,
            ILogger<EmailService> logger)
        {
            _sendGridClient = sendGridClient;
            _logger = logger;
            _fromEmail = configuration.SendGridFromEmail;
            _fromName = configuration.SendGridFromName;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, string? htmlContent = null)
        {
            try
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(
                    from, 
                    to, 
                    subject, 
                    body, 
                    htmlContent ?? body
                );

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email sent successfully to {toEmail}. Subject: {subject}");
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"Failed to send email. Status: {response.StatusCode}, Error: {errorBody}");
                    throw new Exception($"SendGrid API returned {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {toEmail}: {ex.Message}");
                throw;
            }
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string orderId, decimal amount)
        {
            var subject = $"Order Confirmation - {orderId}";
            var plainTextBody = $"Thank you for your order!\n\nOrder ID: {orderId}\nAmount: ${amount:F2}\n\nWe'll process your order shortly.";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Thank you for your order!</h2>
                    <p><strong>Order ID:</strong> {orderId}</p>
                    <p><strong>Amount:</strong> ${amount:F2}</p>
                    <p>We'll process your order shortly.</p>
                    <hr>
                    <p style='color: #666; font-size: 12px;'>This is an automated message. Please do not reply.</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, plainTextBody, htmlContent);
        }
    }
}
