using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.DTOs;
using Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly RoleService _roleService;

        public RolesController(RoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Obtiene todos los roles
        /// </summary>
        /// <returns>Lista de roles</returns>
        [HttpGet]
        [Authorize(Policy = "CanReadRoles")]
        [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }

        /// <summary>
        /// Obtiene un rol por su ID
        /// </summary>
        /// <param name="id">ID del rol</param>
        /// <returns>Rol</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanReadRoles")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDto>> GetById(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Rol no encontrado" });
            }

            return Ok(role);
        }

        /// <summary>
        /// Crea un nuevo rol
        /// </summary>
        /// <param name="createRoleDto">Datos del nuevo rol</param>
        /// <returns>Rol creado</returns>
        [HttpPost]
        [Authorize(Policy = "CanWriteRoles")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto createRoleDto)
        {
            var result = await _roleService.CreateRoleAsync(createRoleDto);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Role!.Id }, result.Role);
        }

        /// <summary>
        /// Actualiza un rol existente
        /// </summary>
        /// <param name="id">ID del rol</param>
        /// <param name="updateRoleDto">Datos actualizados del rol</param>
        /// <returns>Rol actualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanWriteRoles")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDto>> Update(int id, [FromBody] UpdateRoleDto updateRoleDto)
        {
            var result = await _roleService.UpdateRoleAsync(id, updateRoleDto);
            
            if (!result.Success)
            {
                if (result.Message.Contains("no encontrado"))
                {
                    return NotFound(new { message = result.Message });
                }
                return BadRequest(new { message = result.Message });
            }

            return Ok(result.Role);
        }

        /// <summary>
        /// Elimina un rol
        /// </summary>
        /// <param name="id">ID del rol</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteRoles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _roleService.DeleteRoleAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Rol no encontrado" });
            }

            return NoContent();
        }

        /// <summary>
        /// Obtiene los permisos asociados a un rol
        /// </summary>
        /// <param name="id">ID del rol</param>
        /// <returns>Lista de permisos</returns>
        [HttpGet("{id}/permissions")]
        [Authorize(Policy = "CanReadRoles")]
        [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Rol no encontrado" });
            }

            var permissions = await _roleService.GetPermissionsByRoleIdAsync(id);
            return Ok(permissions);
        }
    }
}