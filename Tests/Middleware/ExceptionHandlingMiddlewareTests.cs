using Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Tests.Models;

namespace Tests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
        private readonly ExceptionHandlingMiddleware _middleware;

        public ExceptionHandlingMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _middleware = new ExceptionHandlingMiddleware(next: (innerHttpContext) =>
            {
                return Task.FromException(new Exception("Test exception"));
            }, _loggerMock.Object);
        }

        [Fact]
        public async Task InvokeAsync_WithGenericException_ShouldReturn500()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            context.Response.ContentType.Should().Be("application/json");

            // Verificar el contenido de la respuesta
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            response.Should().NotBeNull();
            response!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
            response.Message.Should().Be("Se ha producido un error interno en el servidor.");
        }

        [Fact]
        public async Task InvokeAsync_WithKeyNotFoundException_ShouldReturn404()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var notFoundMiddleware = new ExceptionHandlingMiddleware(next: (innerHttpContext) =>
            {
                return Task.FromException(new KeyNotFoundException("Recurso no encontrado"));
            }, _loggerMock.Object);

            // Act
            await notFoundMiddleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            context.Response.ContentType.Should().Be("application/json");

            // Verificar el contenido de la respuesta
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            response.Should().NotBeNull();
            response!.Status.Should().Be((int)HttpStatusCode.NotFound);
            response.Message.Should().Be("El recurso solicitado no fue encontrado.");
        }

        [Fact]
        public async Task InvokeAsync_WithUnauthorizedAccessException_ShouldReturn401()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var unauthorizedMiddleware = new ExceptionHandlingMiddleware(next: (innerHttpContext) =>
            {
                return Task.FromException(new UnauthorizedAccessException("No autorizado"));
            }, _loggerMock.Object);

            // Act
            await unauthorizedMiddleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            context.Response.ContentType.Should().Be("application/json");

            // Verificar el contenido de la respuesta
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            response.Should().NotBeNull();
            response!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
            response.Message.Should().Be("No está autorizado para acceder a este recurso.");
        }

        [Fact]
        public async Task InvokeAsync_WithArgumentException_ShouldReturn400()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var errorMessage = "Argumento inválido";

            var badRequestMiddleware = new ExceptionHandlingMiddleware(next: (innerHttpContext) =>
            {
                return Task.FromException(new ArgumentException(errorMessage));
            }, _loggerMock.Object);

            // Act
            await badRequestMiddleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            context.Response.ContentType.Should().Be("application/json");

            // Verificar el contenido de la respuesta
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            response.Should().NotBeNull();
            response!.Status.Should().Be((int)HttpStatusCode.BadRequest);
            response.Message.Should().Be(errorMessage);
        }

        [Fact]
        public async Task InvokeAsync_WithNoException_ShouldNotModifyResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var successMiddleware = new ExceptionHandlingMiddleware(next: (innerHttpContext) =>
            {
                return Task.CompletedTask;
            }, _loggerMock.Object);

            // Act
            await successMiddleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK); // Default status code
            context.Response.ContentType.Should().BeNull(); // No content type set
        }

        // Utilizamos la clase ErrorResponse del namespace Tests.Models
    }
}