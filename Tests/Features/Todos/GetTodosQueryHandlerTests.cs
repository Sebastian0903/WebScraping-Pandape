using Application.DTOs;
using Application.Features.Todos.Queries.GetTodos;
using AutoMapper;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Features.Todos
{
    public class GetTodosQueryHandlerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly DbContextOptions<AppDbContext> _options;

        public GetTodosQueryHandlerTests()
        {
            _mapperMock = new Mock<IMapper>();
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TodoDb_{Guid.NewGuid()}")
                .Options;

            // Inicializar la base de datos con datos de prueba
            using var context = new AppDbContext(_options);
            context.Todos.AddRange(
                new Todo { Id = 1, Title = "Test Todo 1", IsDone = false },
                new Todo { Id = 2, Title = "Test Todo 2", IsDone = true }
            );
            context.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnAllTodos()
        {
            // Arrange
            var query = new GetTodosQuery();
            var expectedTodoDtos = new List<TodoDto>
            {
                new TodoDto { Id = 1, Title = "Test Todo 1", IsDone = false },
                new TodoDto { Id = 2, Title = "Test Todo 2", IsDone = true }
            };

            using var context = new AppDbContext(_options);
            var handler = new GetTodosQueryHandler(context, _mapperMock.Object);

            _mapperMock
                .Setup(m => m.Map<List<TodoDto>>(It.IsAny<List<Todo>>()))
                .Returns(expectedTodoDtos);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedTodoDtos);
            result.Should().HaveCount(2);

            // Verificar que se haya llamado al mapper
            _mapperMock.Verify(m => m.Map<List<TodoDto>>(It.IsAny<List<Todo>>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetTodosQuery();
            var emptyList = new List<TodoDto>();

            // Crear una nueva base de datos vacía
            var emptyDbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"EmptyTodoDb_{Guid.NewGuid()}")
                .Options;

            using var context = new AppDbContext(emptyDbOptions);
            var handler = new GetTodosQueryHandler(context, _mapperMock.Object);

            _mapperMock
                .Setup(m => m.Map<List<TodoDto>>(It.IsAny<List<Todo>>()))
                .Returns(emptyList);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
            result.Should().HaveCount(0);

            // Verificar que se haya llamado al mapper
            _mapperMock.Verify(m => m.Map<List<TodoDto>>(It.IsAny<List<Todo>>()), Times.Once);
        }
    }
}