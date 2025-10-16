using Application.DTOs;
using Application.Features.Todos.Commands.CreateTodo;
using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Features.Todos
{
    public class CreateTodoCommandHandlerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITodoRepository> _todoRepositoryMock;

        public CreateTodoCommandHandlerTests()
        {
            _mapperMock = new Mock<IMapper>();
            _todoRepositoryMock = new Mock<ITodoRepository>();
        }

        [Fact]
        public async Task Handle_ShouldCreateTodoAndReturnDto()
        {
            // Arrange
            var command = new CreateTodoCommand("Test Todo");
            var createdTodo = new Todo { Id = 1, Title = "Test Todo", IsDone = false };
            var expectedTodoDto = new TodoDto { Id = 1, Title = "Test Todo", IsDone = false };

            _todoRepositoryMock
                .Setup(r => r.AddAsync(It.Is<Todo>(t => t.Title == command.Title)))
                .ReturnsAsync(createdTodo);

            _mapperMock
                .Setup(m => m.Map<TodoDto>(It.IsAny<Todo>()))
                .Returns(expectedTodoDto);

            var handler = new CreateTodoCommandHandler(_todoRepositoryMock.Object, _mapperMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedTodoDto);

            // Verificar que se haya llamado al repositorio
            _todoRepositoryMock.Verify(r => r.AddAsync(It.Is<Todo>(t => t.Title == command.Title)), Times.Once);

            // Verificar que se haya llamado al mapper
            _mapperMock.Verify(m => m.Map<TodoDto>(It.Is<Todo>(t => t.Title == command.Title)), Times.Once);
        }
    }
}