using AutoMapper;
using Domain.Repositories;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests
{
    /// <summary>
    /// Clase base para pruebas que proporciona configuraciones comunes
    /// </summary>
    public abstract class TestBase
    {
        protected Mock<IMediator> MediatorMock { get; }
        protected Mock<IMapper> MapperMock { get; }
        protected Mock<IUserRepository> UserRepositoryMock { get; }
        protected Mock<IRoleRepository> RoleRepositoryMock { get; }
        protected Mock<ITodoRepository> TodoRepositoryMock { get; }
        protected DbContextOptions<AppDbContext> DbContextOptions { get; }

        protected TestBase()
        {
            // Inicializar mocks comunes
            MediatorMock = new Mock<IMediator>();
            MapperMock = new Mock<IMapper>();
            UserRepositoryMock = new Mock<IUserRepository>();
            RoleRepositoryMock = new Mock<IRoleRepository>();
            TodoRepositoryMock = new Mock<ITodoRepository>();

            // Configurar base de datos en memoria para pruebas
            DbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
        }

        /// <summary>
        /// Crea una instancia de AppDbContext con la base de datos en memoria
        /// </summary>
        /// <returns>Una nueva instancia de AppDbContext</returns>
        protected AppDbContext CreateDbContext()
        {
            return new AppDbContext(DbContextOptions);
        }
    }
}