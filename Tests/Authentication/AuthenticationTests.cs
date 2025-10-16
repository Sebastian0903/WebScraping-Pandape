using Api.Controllers;
using Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Tests.Authentication
{
    public class AuthenticationTests
    {
        [Fact]
        public async Task Authenticated_Endpoint_Should_Return_Unauthorized_For_Unauthenticated_User()
        {
            // Arrange
            var mediatorMock = new Mock<MediatR.IMediator>();
            var loggerMock = new Mock<ILogger<TodosController>>();
            var controller = new TodosController(mediatorMock.Object, loggerMock.Object);
            
            // Configurar el ControllerContext para simular un usuario no autenticado
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Get());
        }

        [Fact]
        public async Task Authenticated_Endpoint_Should_Allow_Authenticated_User()
        {
            // Arrange
            var mediatorMock = new Mock<MediatR.IMediator>();
            var loggerMock = new Mock<ILogger<TodosController>>();
            var controller = new TodosController(mediatorMock.Object, loggerMock.Object);
            
            // Configurar el ControllerContext para simular un usuario autenticado
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("custom_claim", "custom_value"),
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Configurar el mediator para devolver una lista vacía
            mediatorMock
                .Setup(m => m.Send(It.IsAny<Application.Features.Todos.Queries.GetTodos.GetTodosQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TodoDto>());

            // Act
            var result = await controller.Get();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<TodoDto>>();
        }
    }
}