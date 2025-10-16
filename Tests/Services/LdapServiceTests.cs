using Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.DirectoryServices;
using System.Threading.Tasks;

namespace Tests.Services
{
    public class LdapServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<LdapService>> _loggerMock;

        public LdapServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<LdapService>>();

            // Configurar valores de prueba para la configuración LDAP
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(s => s.Value).Returns("test-ldap-server");
            _configurationMock.Setup(c => c["Ldap:Server"]).Returns("test-ldap-server");
            _configurationMock.Setup(c => c["Ldap:Port"]).Returns("389");
            _configurationMock.Setup(c => c["Ldap:RequiredGroup"]).Returns("test-group");
        }

        [Fact(Skip = "Esta prueba requiere un servidor LDAP real")]
        public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var ldapService = new LdapService(_configurationMock.Object, _loggerMock.Object);
            var username = "test-user";
            var password = "test-password";

            // Act
            var result = await ldapService.AuthenticateAsync(username, password);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("OK");
        }

        [Fact(Skip = "Esta prueba requiere un servidor LDAP real")]
        public async Task AuthenticateAsync_WithInvalidCredentials_ShouldReturnFailure()
        {
            // Arrange
            var ldapService = new LdapService(_configurationMock.Object, _loggerMock.Object);
            var username = "test-user";
            var password = "wrong-password";

            // Act
            var result = await ldapService.AuthenticateAsync(username, password);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("INVALIDCREDENTIALS");
        }

        [Fact(Skip = "Esta prueba requiere un servidor LDAP real")]
        public async Task AuthenticateAsync_WithNonExistentUser_ShouldReturnNotFound()
        {
            // Arrange
            var ldapService = new LdapService(_configurationMock.Object, _loggerMock.Object);
            var username = "non-existent-user";
            var password = "test-password";

            // Act
            var result = await ldapService.AuthenticateAsync(username, password);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("NOTFOUND");
        }

        [Fact(Skip = "Esta prueba requiere un servidor LDAP real")]
        public async Task AuthenticateAsync_WithUserNotInRequiredGroup_ShouldReturnNotAMember()
        {
            // Arrange
            var ldapService = new LdapService(_configurationMock.Object, _loggerMock.Object);
            var username = "user-without-group";
            var password = "test-password";

            // Act
            var result = await ldapService.AuthenticateAsync(username, password);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("NOTAMEMBER");
        }

        [Fact(Skip = "Esta prueba requiere un servidor LDAP real")]
        public async Task AuthenticateAsync_WithDisabledAccount_ShouldReturnDisabledAccount()
        {
            // Arrange
            var ldapService = new LdapService(_configurationMock.Object, _loggerMock.Object);
            var username = "disabled-user";
            var password = "test-password";

            // Act
            var result = await ldapService.AuthenticateAsync(username, password);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("DISABLEDACCOUNT");
        }
    }
}