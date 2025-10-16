using Application.Features.Todos.Commands.DeleteTodo;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Tests.Features.Todos
{
    public class DeleteTodoCommandHandlerTests
    {
        private readonly Mock<ITodoRepository> _todoRepositoryMock;

        public DeleteTodoCommandHandlerTests()
        {
            _todoRepositoryMock = new Mock<ITodoRepository>();
        }

        [Fact]
        public async Task Handle_WithExistingTodo_ShouldDeleteTodo()
        {
            // Arrange
            var command = new DeleteTodoCommand(1);
            var existingTodo = new Todo { Id = 1, Title = "Todo to Delete", IsDone = false };

            _todoRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingTodo);

            var handler = new DeleteTodoCommandHandler(_todoRepositoryMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            _todoRepositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistingTodo_ShouldThrowException()
        {
            // Arrange
            var command = new DeleteTodoCommand(999);

            _todoRepositoryMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Todo)null);

            var handler = new DeleteTodoCommandHandler(_todoRepositoryMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

            // Verificar que no se llamó al método DeleteAsync
            _todoRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}