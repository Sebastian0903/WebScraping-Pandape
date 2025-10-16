using Api.Controllers;
using Application.DTOs;
using Application.Features.Todos.Commands.CreateTodo;
using Application.Features.Todos.Commands.DeleteTodo;
using Application.Features.Todos.Commands.UpdateTodo;
using Application.Features.Todos.Queries.GetTodos;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Unit = MediatR.Unit;

namespace Tests.Controllers
{
    public class TodosControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ILogger<TodosController>> _loggerMock;
        private readonly TodosController _controller;

        public TodosControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mediatorMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Get_ShouldReturnOkWithTodos()
        {
            // Arrange
            var expectedTodos = new List<TodoDto>
            {
                new TodoDto { Id = 1, Title = "Test Todo 1", IsDone = false },
                new TodoDto { Id = 2, Title = "Test Todo 2", IsDone = true }
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetTodosQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTodos);

            // Act
            var result = await _controller.Get();

            // Assert
            // El controlador devuelve directamente la lista, no un ActionResult
            result.Should().BeEquivalentTo(expectedTodos);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedWithTodo()
        {
            // Arrange
            var command = new CreateTodoCommand("New Todo");
            var createdTodo = new TodoDto { Id = 1, Title = "New Todo", IsDone = false };

            _mediatorMock
                .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTodo);

            // Act
            var result = await _controller.Create(command);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(_controller.Get));
            createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(createdTodo.Id);
            createdResult.Value.Should().BeEquivalentTo(createdTodo);
        }

        [Fact]
        public async Task Update_WithMatchingIds_ShouldReturnOkWithTodo()
        {
            // Arrange
            var todoId = 1;
            var command = new UpdateTodoCommand { Id = todoId, Title = "Updated Todo", IsDone = true };
            var updatedTodo = new TodoDto { Id = todoId, Title = "Updated Todo", IsDone = true };

            _mediatorMock
                .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedTodo);

            // Act
            var result = await _controller.Update(todoId, command);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(updatedTodo);
        }

        [Fact]
        public async Task Update_WithMismatchingIds_ShouldReturnBadRequest()
        {
            // Arrange
            var todoId = 1;
            var command = new UpdateTodoCommand { Id = 2, Title = "Updated Todo", IsDone = true };

            // Act
            var result = await _controller.Update(todoId, command);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent()
        {
            // Arrange
            var todoId = 1;

            _mediatorMock
                .Setup(m => m.Send(It.Is<DeleteTodoCommand>(c => c.Id == todoId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.Delete(todoId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mediatorMock.Verify(m => m.Send(It.Is<DeleteTodoCommand>(c => c.Id == todoId), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}