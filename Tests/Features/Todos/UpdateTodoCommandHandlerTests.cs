using Application.DTOs;
using Application.Features.Todos.Commands.UpdateTodo;
using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Features.Todos
{
    public class UpdateTodoCommandHandlerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITodoRepository> _todoRepositoryMock;

        public UpdateTodoCommandHandlerTests()
        {
            _mapperMock = new Mock<IMapper>();
            _todoRepositoryMock = new Mock<ITodoRepository>();
        }

        [Fact]
        public async Task Handle_WithExistingTodo_ShouldUpdateAndReturnDto()
        {
            // Arrange
            var command = new UpdateTodoCommand { Id = 1, Title = "Updated Todo", IsDone = true };
            var existingTodo = new Todo { Id = 1, Title = "Original Todo", IsDone = false };
            var expectedTodoDto = new TodoDto { Id = 1, Title = "Updated Todo", IsDone = true };

            _todoRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingTodo);

            _mapperMock
                .Setup(m => m.Map<TodoDto>(It.IsAny<Todo>()))
                .Returns(expectedTodoDto);

            var handler = new UpdateTodoCommandHandler(_todoRepositoryMock.Object, _mapperMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedTodoDto);

            // Verificar que se haya llamado al repositorio para actualizar
            _todoRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Todo>(t => 
                t.Id == command.Id && 
                t.Title == command.Title && 
                t.IsDone == command.IsDone)), Times.Once);

            // Verificar que se haya llamado al mapper
            _mapperMock.Verify(m => m.Map<TodoDto>(It.Is<Todo>(t => t.Id == command.Id && t.Title == command.Title)), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistingTodo_ShouldThrowException()
        {
            // Arrange
            var command = new UpdateTodoCommand { Id = 999, Title = "Non-existent Todo", IsDone = true };

            _todoRepositoryMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Todo)null);

            var handler = new UpdateTodoCommandHandler(_todoRepositoryMock.Object, _mapperMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

            // Verificar que no se haya llamado al repositorio para actualizar
            _todoRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Todo>()), Times.Never);

            // Verificar que no se haya llamado al mapper
            _mapperMock.Verify(m => m.Map<TodoDto>(It.IsAny<Todo>()), Times.Never);
        }
    }
}