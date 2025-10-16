using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories
{
    public class TodoRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TodoRepositoryTests()
        {
            // Configurar la base de datos en memoria para pruebas
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TodoDb_{Guid.NewGuid()}")
                .Options;

            // Inicializar la base de datos con datos de prueba
            using var context = new AppDbContext(_options);
            context.Todos.AddRange(
                new Todo { Id = 1, Title = "Test Todo 1", IsDone = false },
                new Todo { Id = 2, Title = "Test Todo 2", IsDone = true },
                new Todo { Id = 3, Title = "Test Todo 3", IsDone = false }
            );
            context.SaveChanges();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTodos()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new TodoRepository(context);

            // Act
            var todos = await repository.GetAllAsync();

            // Assert
            todos.Should().HaveCount(3);
            todos.Select(t => t.Id).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnTodo()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new TodoRepository(context);

            // Act
            var todo = await repository.GetByIdAsync(2);

            // Assert
            todo.Should().NotBeNull();
            todo!.Id.Should().Be(2);
            todo.Title.Should().Be("Test Todo 2");
            todo.IsDone.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new TodoRepository(context);

            // Act
            var todo = await repository.GetByIdAsync(999);

            // Assert
            todo.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldAddTodoAndReturnIt()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new TodoRepository(context);
            var newTodo = new Todo { Title = "New Todo", IsDone = false };

            // Act
            var result = await repository.AddAsync(newTodo);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Title.Should().Be("New Todo");

            // Verificar que se haya agregado a la base de datos
            var todoInDb = await context.Todos.FindAsync(result.Id);
            todoInDb.Should().NotBeNull();
            todoInDb!.Title.Should().Be("New Todo");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTodo()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new TodoRepository(context);
            var todoToUpdate = await context.Todos.FindAsync(1);
            todoToUpdate!.Title = "Updated Todo";
            todoToUpdate.IsDone = true;

            // Act
            await repository.UpdateAsync(todoToUpdate);

            // Assert
            var updatedTodo = await context.Todos.FindAsync(1);
            updatedTodo.Should().NotBeNull();
            updatedTodo!.Title.Should().Be("Updated Todo");
            updatedTodo.IsDone.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_ShouldDeleteTodo()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new TodoRepository(context);

            // Act
            await repository.DeleteAsync(3);

            // Assert
            var deletedTodo = await context.Todos.FindAsync(3);
            deletedTodo.Should().BeNull();

            // Verificar que los otros todos siguen existiendo
            var remainingTodos = await context.Todos.ToListAsync();
            remainingTodos.Should().HaveCount(2);
            remainingTodos.Select(t => t.Id).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingId_ShouldNotThrowException()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new TodoRepository(context);

            // Act & Assert
            await repository.DeleteAsync(999);
            // No debería lanzar excepción
        }
    }
}