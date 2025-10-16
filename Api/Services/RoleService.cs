using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Services
{
    public class RoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;

        public RoleService(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(MapToDto);
        }

        public async Task<RoleDto?> GetRoleByIdAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            return role != null ? MapToDto(role) : null;
        }

        public async Task<RoleDto?> GetRoleByNameAsync(string name)
        {
            var role = await _roleRepository.GetByNameAsync(name);
            return role != null ? MapToDto(role) : null;
        }

        public async Task<(bool Success, string Message, RoleDto? Role)> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            // Verificar si el rol ya existe
            var existingRole = await _roleRepository.GetByNameAsync(createRoleDto.Name);
            if (existingRole != null)
            {
                return (false, "El nombre del rol ya está en uso", null);
            }

            // Crear el rol
            var role = new Role
            {
                Name = createRoleDto.Name,
                Description = createRoleDto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Asignar permisos si se proporcionan
            if (createRoleDto.PermissionIds != null && createRoleDto.PermissionIds.Any())
            {
                var permissions = await _permissionRepository.GetAllAsync();
                var filteredPermissions = permissions.Where(p => createRoleDto.PermissionIds.Contains(p.Id));
                
                foreach (var permission in filteredPermissions)
                {
                    role.Permissions.Add(new RolePermission
                    {
                        PermissionId = permission.Id,
                        Permission = permission
                    });
                }
            }

            await _roleRepository.CreateAsync(role);
            return (true, "Rol creado exitosamente", MapToDto(role));
        }

        public async Task<(bool Success, string Message, RoleDto? Role)> UpdateRoleAsync(int id, UpdateRoleDto updateRoleDto)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                return (false, "Rol no encontrado", null);
            }

            // Actualizar nombre si se proporciona
            if (!string.IsNullOrEmpty(updateRoleDto.Name) && updateRoleDto.Name != role.Name)
            {
                var existingRole = await _roleRepository.GetByNameAsync(updateRoleDto.Name);
                if (existingRole != null && existingRole.Id != id)
                {
                    return (false, "El nombre del rol ya está en uso", null);
                }
                role.Name = updateRoleDto.Name;
            }

            // Actualizar descripción si se proporciona
            if (!string.IsNullOrEmpty(updateRoleDto.Description))
            {
                role.Description = updateRoleDto.Description;
            }

            // Actualizar estado activo si se proporciona
            if (updateRoleDto.IsActive.HasValue)
            {
                role.IsActive = updateRoleDto.IsActive.Value;
            }

            // Actualizar permisos si se proporcionan
            if (updateRoleDto.PermissionIds != null)
            {
                // Eliminar permisos actuales
                role.Permissions.Clear();

                // Asignar nuevos permisos
                if (updateRoleDto.PermissionIds != null && updateRoleDto.PermissionIds.Any())
                {
                    var permissions = await _permissionRepository.GetAllAsync();
                    var filteredPermissions = permissions.Where(p => updateRoleDto.PermissionIds.Contains(p.Id));
                    
                    foreach (var permission in filteredPermissions)
                    {
                        role.Permissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permission.Id,
                            Permission = permission
                        });
                    }
                }
            }

            await _roleRepository.UpdateAsync(role);
            return (true, "Rol actualizado exitosamente", MapToDto(role));
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                return false;
            }

            await _roleRepository.DeleteAsync(id);
            return true;
        }

        public async Task<List<PermissionDto>> GetPermissionsByRoleIdAsync(int roleId)
        {
            var permissions = await _roleRepository.GetPermissionsByRoleIdAsync(roleId);
            return permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                Category = p.Category,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        // Métodos privados para mapeo
        private RoleDto MapToDto(Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                Permissions = role.Permissions?.Select(rp => new PermissionDto { Id = rp.Permission.Id, Name = rp.Permission.Name, Code = rp.Permission.Code, Description = rp.Permission.Description }).ToList() ?? new List<PermissionDto>()
            };
        }
    }
}