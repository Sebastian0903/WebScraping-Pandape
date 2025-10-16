using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Registrar DbContext
            services.AddDbContext<Infrastructure.Persistence.AppDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(Infrastructure.Persistence.AppDbContext).Assembly.FullName)));

            // Registrar repositorios
            services.AddScoped<ITodoRepository, TodoRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            
            // Registrar DataSeeder
            services.AddScoped<DataSeeder>();

            return services;
        }
    }
}