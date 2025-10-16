using Api.Controllers;
using Api.Services;
using Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<UserService> _userServiceMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Configurar mock de IConfiguration
            _configurationMock = new Mock<IConfiguration>();
            var configurationSectionMock = new Mock<IConfigurationSection>();
            configurationSectionMock.Setup(x => x.Value).Returns("test-secret-key-with-at-least-32-characters");
            _configurationMock.Setup(x => x["Jwt:Key"]).Returns("test-secret-key-with-at-least-32-characters");
            _configurationMock.Setup(x => x["Jwt:DurationInMinutes"]).Returns("60");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("test-audience");

            // Configurar mock de UserService
            var userRepositoryMock = new Mock<Domain.Repositories.IUserRepository>();
            var roleRepositoryMock = new Mock<Domain.Repositories.IRoleRepository>();
            var ldapConfigMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var ldapServiceMock = new Mock<LdapService>(MockBehavior.Loose, ldapConfigMock.Object);
            var loggerMock = new Mock<ILogger<UserService>>();
            _userServiceMock = new Mock<UserService>(MockBehavior.Loose, userRepositoryMock.Object, roleRepositoryMock.Object, ldapServiceMock.Object, loggerMock.Object);

            // Crear controlador con mocks
            _controller = new AuthController(_configurationMock.Object, _userServiceMock.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
        {
            // Arrange
            var loginDto = new UserLoginDto { UsernameOrDocumentNumber = "testuser", Password = "password" };
            var userDto = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                RoleId = 1,
                RoleName = "User"
            };

            _userServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<UserLoginDto>()))
                .ReturnsAsync((true, "Autenticación exitosa", userDto));

            _userServiceMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(userDto);

            _userServiceMock.Setup(x => x.GetPermissionsByRoleIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "read", "write" });

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<UserLoginResponseDto>().Subject;
            response.Should().NotBeNull();
            response.Token.Should().NotBeNullOrEmpty();
            response.User.Should().NotBeNull();
            response.User.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new UserLoginDto { UsernameOrDocumentNumber = "testuser", Password = "wrongpassword" };

            _userServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<UserLoginDto>()))
                .ReturnsAsync((false, "Usuario o contraseña incorrectos", null));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var response = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
            var message = response.GetType().GetProperty("message")?.GetValue(response, null);
            message.Should().Be("Usuario o contraseña incorrectos");
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnCreatedWithToken()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Username = "newuser",
                Email = "new@example.com",
                Password = "password",
                RoleId = 1
            };

            var userDto = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "newuser",
                Email = "new@example.com",
                RoleId = 1,
                RoleName = "User"
            };

            _userServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>()))
                .ReturnsAsync((true, "Usuario creado exitosamente", userDto));

            _userServiceMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(userDto);

            _userServiceMock.Setup(x => x.GetPermissionsByRoleIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "read", "write" });

            // Act
            var result = await _controller.Register(createUserDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
            var response = createdResult.Value.Should().BeAssignableTo<UserLoginResponseDto>().Subject;
            response.Should().NotBeNull();
            response.Token.Should().NotBeNullOrEmpty();
            response.User.Should().NotBeNull();
            response.User.Username.Should().Be("newuser");
        }

        [Fact]
        public async Task Register_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Username = "existinguser",
                Email = "existing@example.com",
                Password = "password",
                RoleId = 1
            };

            _userServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>()))
                .ReturnsAsync((false, "El nombre de usuario ya está en uso", null));

            // Act
            var result = await _controller.Register(createUserDto);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
            var message = response.GetType().GetProperty("message")?.GetValue(response, null);
            message.Should().Be("El nombre de usuario ya está en uso");
        }

        [Fact]
        public void ValidateToken_WithValidToken_ShouldReturnTokenInfo()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = _controller.ValidateToken();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<TokenValidationResponse>().Subject;
            response.Should().NotBeNull();
            response.IsValid.Should().BeTrue();
            response.Username.Should().Be("testuser");
            response.Roles.Should().ContainSingle().Which.Should().Be("Admin");
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldReturnNewToken()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "User")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var userDto = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                RoleId = 1,
                RoleName = "User"
            };

            _userServiceMock.Setup(x => x.GetUserByUsernameAsync("testuser"))
                .ReturnsAsync(userDto);

            _userServiceMock.Setup(x => x.GetPermissionsByRoleIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "read", "write" });

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<UserLoginResponseDto>().Subject;
            response.Should().NotBeNull();
            response.Token.Should().NotBeNullOrEmpty();
            response.User.Should().NotBeNull();
            response.User.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task RefreshToken_WithInvalidUsername_ShouldReturnUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "User")
                // Sin nombre de usuario
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var response = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
            var message = response.GetType().GetProperty("message")?.GetValue(response, null);
            message.Should().Be("Token inválido");
        }

        [Fact]
        public async Task RefreshToken_WithNonExistentUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "nonexistentuser"),
                new Claim(ClaimTypes.Role, "User")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _userServiceMock.Setup(x => x.GetUserByUsernameAsync("nonexistentuser"))
                .ReturnsAsync((UserDto)null);

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var response = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
            var message = response.GetType().GetProperty("message")?.GetValue(response, null);
            message.Should().Be("Usuario no encontrado");
        }
    }
}