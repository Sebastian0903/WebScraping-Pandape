using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Services
{
    public class PermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _permissionRepository.GetAllAsync();
            return permissions.Select(MapToDto);
        }

        public async Task<PermissionDto?> GetPermissionByIdAsync(Guid id)
        {
            var permission = await _permissionRepository.GetByIdAsync(id);
            return permission != null ? MapToDto(permission) : null;
        }

        public async Task<PermissionDto?> GetPermissionByCodeAsync(string code)
        {
            var permission = await _permissionRepository.GetByCodeAsync(code);
            return permission != null ? MapToDto(permission) : null;
        }

        public async Task<(bool Success, string Message, PermissionDto? Permission)> CreatePermissionAsync(CreatePermissionDto createPermissionDto)
        {
            // Verificar si el código ya existe
            var existingPermission = await _permissionRepository.GetByCodeAsync(createPermissionDto.Code);
            if (existingPermission != null)
            {
                return (false, "El código del permiso ya está en uso", null);
            }

            // Crear el permiso
            var permission = new Permission
            {
                // El Id será asignado automáticamente por la base de datos
                Name = createPermissionDto.Name,
                Code = createPermissionDto.Code,
                Description = createPermissionDto.Description,
                Category = createPermissionDto.Category,
                CreatedAt = DateTime.UtcNow
            };

            await _permissionRepository.CreateAsync(permission);
            return (true, "Permiso creado exitosamente", MapToDto(permission));
        }

        public async Task<(bool Success, string Message, PermissionDto? Permission)> UpdatePermissionAsync(Guid id, UpdatePermissionDto updatePermissionDto)
        {
            var permission = await _permissionRepository.GetByIdAsync(id);
            if (permission == null)
            {
                return (false, "Permiso no encontrado", null);
            }

            // Actualizar código si se proporciona
            if (!string.IsNullOrEmpty(updatePermissionDto.Code) && updatePermissionDto.Code != permission.Code)
            {
                var existingPermission = await _permissionRepository.GetByCodeAsync(updatePermissionDto.Code);
                if (existingPermission != null && existingPermission.Id != id)
                {
                    return (false, "El código del permiso ya está en uso", null);
                }
                permission.Code = updatePermissionDto.Code;
            }

            // Actualizar nombre si se proporciona
            if (!string.IsNullOrEmpty(updatePermissionDto.Name))
            {
                permission.Name = updatePermissionDto.Name;
            }

            // Actualizar descripción si se proporciona
            if (!string.IsNullOrEmpty(updatePermissionDto.Description))
            {
                permission.Description = updatePermissionDto.Description;
            }

            // Actualizar categoría si se proporciona
            if (!string.IsNullOrEmpty(updatePermissionDto.Category))
            {
                permission.Category = updatePermissionDto.Category;
            }

            await _permissionRepository.UpdateAsync(permission);
            return (true, "Permiso actualizado exitosamente", MapToDto(permission));
        }

        public async Task<bool> DeletePermissionAsync(Guid id)
        {
            var permission = await _permissionRepository.GetByIdAsync(id);
            if (permission == null)
            {
                return false;
            }

            await _permissionRepository.DeleteAsync(id);
            return true;
        }

        // Métodos privados para mapeo
        private PermissionDto MapToDto(Permission permission)
        {
            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Code = permission.Code,
                Description = permission.Description,
                Category = permission.Category,
                CreatedAt = permission.CreatedAt
            };
        }
    }
}