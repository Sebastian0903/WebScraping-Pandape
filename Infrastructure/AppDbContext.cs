using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		// Entidades
		public DbSet<Todo> Todos => Set<Todo>();
		public DbSet<User> Users => Set<User>();
		public DbSet<UserPandape> UsersPandape => Set<UserPandape>();
		public DbSet<PandapeCandidates> PandapeCandidates => Set<PandapeCandidates>();
		public DbSet<Stages> Stages => Set<Stages>();
		public DbSet<Vacancies> Vacancies => Set<Vacancies>();
		public DbSet<Role> Roles => Set<Role>();
		public DbSet<Permission> Permissions => Set<Permission>();
		public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Todo>(e =>
			{
				e.HasKey(x => x.Id);
				e.Property(x => x.Title).HasMaxLength(200).IsRequired();
				e.Property(x => x.IsDone).HasDefaultValue(false);
			});

			modelBuilder.Entity<User>(e =>
			{
				e.HasKey(x => x.Id);
				e.Property(x => x.Username).HasMaxLength(50).IsRequired();
				e.Property(x => x.Email).HasMaxLength(100).IsRequired();
				e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
				e.Property(x => x.Salt).HasMaxLength(200).IsRequired();
				e.Property(x => x.IsActive).HasDefaultValue(true);
				e.Property(x => x.CreatedAt).IsRequired();
				
				// Relación con Role
				e.HasOne(x => x.Role)
				 .WithMany(r => r.Users)
				 .HasForeignKey(x => x.RoleId)
				 .OnDelete(DeleteBehavior.Restrict);
				
				// Índices para búsqueda rápida
				e.HasIndex(x => x.Username).IsUnique();
				e.HasIndex(x => x.Email).IsUnique();
			});
			
			modelBuilder.Entity<Role>(e =>
			{
				e.HasKey(x => x.Id);
				e.Property(x => x.Name).HasMaxLength(50).IsRequired();
				e.Property(x => x.Description).HasMaxLength(200);
				e.Property(x => x.IsActive).HasDefaultValue(true);
				e.Property(x => x.CreatedAt).IsRequired();
				
				// Índice para búsqueda rápida
				e.HasIndex(x => x.Name).IsUnique();
			});
			
			modelBuilder.Entity<Permission>(e =>
			{
				e.HasKey(x => x.Id);
				e.Property(x => x.Name).HasMaxLength(50).IsRequired();
				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Description).HasMaxLength(200);
				e.Property(x => x.Category).HasMaxLength(50);
				e.Property(x => x.CreatedAt).IsRequired();
				
				// Índices para búsqueda rápida
				e.HasIndex(x => x.Code).IsUnique();
			});
			
			modelBuilder.Entity<RolePermission>(e =>
			{
				// Clave primaria compuesta
				e.HasKey(x => new { x.RoleId, x.PermissionId });
				
				// Relaciones
				e.HasOne(x => x.Role)
				 .WithMany(r => r.Permissions)
				 .HasForeignKey(x => x.RoleId);
				 
				e.HasOne(x => x.Permission)
				 .WithMany(p => p.Roles)
				 .HasForeignKey(x => x.PermissionId);
			});
		}
	}
}
