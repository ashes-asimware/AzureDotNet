using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ServiceBusProducerApp.Services;
using AzureDotNet.Shared.Configuration;

namespace ServiceBusProducerApp.Tests.Services
{
    public class ServiceBusServiceTests
    {
        private readonly Mock<IAzureConfigurationProvider> _mockConfig;
        private readonly Mock<ILogger<ServiceBusService>> _mockLogger;

        public ServiceBusServiceTests()
        {
            _mockConfig = new Mock<IAzureConfigurationProvider>();
            _mockLogger = new Mock<ILogger<ServiceBusService>>();
        }

        [Fact]
        public void Constructor_WithConnectionString_CreatesClientSuccessfully()
        {
            // Arrange
            var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Test;SharedAccessKey=testkey";
            _mockConfig.Setup(c => c.ServiceBusConnection).Returns(connectionString);

            // Act
            var service = new ServiceBusService(_mockConfig.Object, _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithFullyQualifiedNamespace_CreatesClientWithManagedIdentity()
        {
            // Arrange
            var fullyQualifiedNamespace = "test.servicebus.windows.net";
            _mockConfig.Setup(c => c.ServiceBusConnection).Returns(fullyQualifiedNamespace);

            // Act
            var service = new ServiceBusService(_mockConfig.Object, _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(
                () => new ServiceBusService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithEmptyConnectionString_ThrowsException()
        {
            // Arrange
            _mockConfig.Setup(c => c.ServiceBusConnection).Throws<InvalidOperationException>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => new ServiceBusService(_mockConfig.Object, _mockLogger.Object));
        }
    }
}
