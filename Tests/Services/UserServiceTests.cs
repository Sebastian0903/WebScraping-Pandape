using Api.Services;
using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<LdapService> _ldapServiceMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _roleRepositoryMock = new Mock<IRoleRepository>();
            var configurationMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _ldapServiceMock = new Mock<LdapService>(MockBehavior.Loose, configurationMock.Object);
            _loggerMock = new Mock<ILogger<UserService>>();
            _userService = new UserService(_userRepositoryMock.Object, _roleRepositoryMock.Object, _ldapServiceMock.Object, _loggerMock.Object, _unitOfWork.Object);
        }

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user1",
                    Email = "user1@example.com",
                    Role = new Role { Name = "User" }
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user2",
                    Email = "user2@example.com",
                    Role = new Role { Name = "Admin" }
                }
            };

            _userRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Select(u => u.Username).Should().BeEquivalentTo(users.Select(u => u.Username));
        }

        [Fact]
        public async Task GetUserByIdAsync_WithExistingId_ShouldReturnUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Role = new Role { Name = "User" }
            };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be(user.Username);
            result.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateUserAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var roleId = 1; // Role.Id es int, no Guid
            var createUserDto = new CreateUserDto
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "Password123!",
                RoleId = roleId
            };

            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(createUserDto.Username)).ReturnsAsync((User?)null);
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(createUserDto.Email)).ReturnsAsync((User?)null);
            _roleRepositoryMock.Setup(repo => repo.GetByIdAsync(roleId)).ReturnsAsync(new Role { Id = roleId, Name = "User" });
            _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>())).ReturnsAsync((User user) => user);

            // Act
            var result = await _userService.CreateUserAsync(createUserDto);

            // Assert
            result.Success.Should().BeTrue();
            result.User.Should().NotBeNull();
            result.User!.Username.Should().Be(createUserDto.Username);
            result.User.Email.Should().Be(createUserDto.Email);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_WithExistingUsername_ShouldReturnError()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Username = "existinguser",
                Email = "newuser@example.com",
                Password = "Password123!",
                RoleId = 1
            };

            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(createUserDto.Username))
                .ReturnsAsync(new User { Username = createUserDto.Username });

            // Act
            var result = await _userService.CreateUserAsync(createUserDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("nombre de usuario ya está en uso");
            result.User.Should().BeNull();
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task CreateUserAsync_WithExistingEmail_ShouldReturnError()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Username = "newuser",
                Email = "existing@example.com",
                Password = "Password123!",
                RoleId = 1
            };

            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(createUserDto.Username)).ReturnsAsync((User?)null);
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(createUserDto.Email))
                .ReturnsAsync(new User { Email = createUserDto.Email });

            // Act
            var result = await _userService.CreateUserAsync(createUserDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("correo electrónico ya está en uso");
            result.User.Should().BeNull();
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
        }
    }
}