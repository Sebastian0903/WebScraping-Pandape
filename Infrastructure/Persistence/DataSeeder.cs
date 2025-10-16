using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public class DataSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Asegurarse de que la base de datos esté creada y actualizada
                await _context.Database.MigrateAsync();

                // Sembrar datos en orden de dependencia
                await SeedRolesAsync();
                await SeedPermissionsAsync();
                await SeedRolePermissionsAsync();
                await SeedUsersAsync();

                _logger.LogInformation("Datos iniciales sembrados correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sembrar datos iniciales");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            if (!await _context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role
                    {
                        Name = "Administrador",
                        Description = "Acceso completo al sistema",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Role
                    {
                        Name = "Usuario",
                        Description = "Acceso limitado al sistema",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Role
                    {
                        Name = "Invitado",
                        Description = "Acceso de solo lectura",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.Roles.AddRangeAsync(roles);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Roles sembrados: {0}", roles.Count);
            }
            else
            {
                _logger.LogInformation("Los roles ya existen en la base de datos");
            }
        }

        private async Task SeedPermissionsAsync()
        {
            if (!await _context.Permissions.AnyAsync())
            {
                var permissions = new List<Permission>
                {
                    // Permisos para usuarios
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ver usuarios",
                        Code = "users.read",
                        Description = "Permite ver la lista de usuarios",
                        Category = "Usuarios",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Crear/Editar usuarios",
                        Code = "users.write",
                        Description = "Permite crear y editar usuarios",
                        Category = "Usuarios",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Eliminar usuarios",
                        Code = "users.delete",
                        Description = "Permite eliminar usuarios",
                        Category = "Usuarios",
                        CreatedAt = DateTime.UtcNow
                    },

                    // Permisos para roles
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ver roles",
                        Code = "roles.read",
                        Description = "Permite ver la lista de roles",
                        Category = "Roles",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Crear/Editar roles",
                        Code = "roles.write",
                        Description = "Permite crear y editar roles",
                        Category = "Roles",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Eliminar roles",
                        Code = "roles.delete",
                        Description = "Permite eliminar roles",
                        Category = "Roles",
                        CreatedAt = DateTime.UtcNow
                    },

                    // Permisos para permisos
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ver permisos",
                        Code = "permissions.read",
                        Description = "Permite ver la lista de permisos",
                        Category = "Permisos",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Crear/Editar permisos",
                        Code = "permissions.write",
                        Description = "Permite crear y editar permisos",
                        Category = "Permisos",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Eliminar permisos",
                        Code = "permissions.delete",
                        Description = "Permite eliminar permisos",
                        Category = "Permisos",
                        CreatedAt = DateTime.UtcNow
                    },

                    // Permisos para contenido
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ver contenido",
                        Code = "content.read",
                        Description = "Permite ver contenido",
                        Category = "Contenido",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Crear/Editar contenido",
                        Code = "content.write",
                        Description = "Permite crear y editar contenido",
                        Category = "Contenido",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Eliminar contenido",
                        Code = "content.delete",
                        Description = "Permite eliminar contenido",
                        Category = "Contenido",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.Permissions.AddRangeAsync(permissions);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Permisos sembrados: {0}", permissions.Count);
            }
            else
            {
                _logger.LogInformation("Los permisos ya existen en la base de datos");
            }
        }

        private async Task SeedRolePermissionsAsync()
        {
            // Verificar si ya existen asignaciones de permisos a roles
            if (!await _context.RolePermissions.AnyAsync())
            {
                // Obtener roles por nombre
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrador");
                var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Usuario");
                var guestRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Invitado");

                // Obtener todos los permisos
                var allPermissions = await _context.Permissions.ToListAsync();
                var readPermissions = allPermissions.Where(p => p.Code.EndsWith(".read")).ToList();

                if (adminRole != null && userRole != null && guestRole != null)
                {
                    var rolePermissions = new List<RolePermission>();

                    // Asignar todos los permisos al rol de Administrador
                    foreach (var permission in allPermissions)
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = adminRole.Id,
                            PermissionId = permission.Id
                        });
                    }

                    // Asignar permisos de lectura y escritura al rol de Usuario (excepto permisos de eliminar)
                    foreach (var permission in allPermissions.Where(p => !p.Code.EndsWith(".delete")))
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = userRole.Id,
                            PermissionId = permission.Id
                        });
                    }

                    // Asignar solo permisos de lectura al rol de Invitado
                    foreach (var permission in readPermissions)
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = guestRole.Id,
                            PermissionId = permission.Id
                        });
                    }

                    await _context.RolePermissions.AddRangeAsync(rolePermissions);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Asignaciones de permisos a roles sembradas: {0}", rolePermissions.Count);
                }
                else
                {
                    _logger.LogWarning("No se pudieron encontrar todos los roles necesarios para asignar permisos");
                }
            }
            else
            {
                _logger.LogInformation("Las asignaciones de permisos a roles ya existen en la base de datos");
            }
        }

        private async Task SeedUsersAsync()
        {
            if (!await _context.Users.AnyAsync())
            {
                // Obtener el rol de administrador
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrador");
                var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Usuario");

                if (adminRole != null && userRole != null)
                {
                    // Crear usuario administrador
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@example.com",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        RoleId = adminRole.Id
                    };

                    // Generar salt y hash para la contraseña "Admin123!"
                    (adminUser.PasswordHash, adminUser.Salt) = GeneratePasswordHash("Admin123!");

                    // Crear usuario normal
                    var normalUser = new User
                    {
                        Username = "usuario",
                        Email = "usuario@example.com",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        RoleId = userRole.Id
                    };

                    // Generar salt y hash para la contraseña "Usuario123!"
                    (normalUser.PasswordHash, normalUser.Salt) = GeneratePasswordHash("Usuario123!");

                    await _context.Users.AddRangeAsync(new[] { adminUser, normalUser });
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Usuarios sembrados: 2");
                }
                else
                {
                    _logger.LogWarning("No se pudieron encontrar los roles necesarios para crear usuarios");
                }
            }
            else
            {
                _logger.LogInformation("Los usuarios ya existen en la base de datos");
            }
        }

        private (string hash, string salt) GeneratePasswordHash(string password)
        {
            // Generar un salt aleatorio
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Generar hash con el salt
            string hash = ComputeHash(password, salt);

            return (hash, salt);
        }

        private string ComputeHash(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                // Combinar contraseña y salt
                var saltedPassword = string.Concat(password, salt);
                var saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
                
                // Calcular hash
                var hashBytes = sha256.ComputeHash(saltedPasswordBytes);
                
                // Convertir a string base64
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}