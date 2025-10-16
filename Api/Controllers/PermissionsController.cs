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
    public class PermissionsController : ControllerBase
    {
        private readonly PermissionService _permissionService;

        public PermissionsController(PermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// Obtiene todos los permisos
        /// </summary>
        /// <returns>Lista de permisos</returns>
        [HttpGet]
        [Authorize(Policy = "CanReadPermissions")]
        [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAll()
        {
            var permissions = await _permissionService.GetAllPermissionsAsync();
            return Ok(permissions);
        }

        /// <summary>
        /// Obtiene un permiso por su ID
        /// </summary>
        /// <param name="id">ID del permiso</param>
        /// <returns>Permiso</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanReadPermissions")]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PermissionDto>> GetById(Guid id)
        {
            var permission = await _permissionService.GetPermissionByIdAsync(id);
            if (permission == null)
            {
                return NotFound(new { message = "Permiso no encontrado" });
            }

            return Ok(permission);
        }

        /// <summary>
        /// Obtiene un permiso por su código
        /// </summary>
        /// <param name="code">Código del permiso</param>
        /// <returns>Permiso</returns>
        [HttpGet("code/{code}")]
        [Authorize(Policy = "CanReadPermissions")]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PermissionDto>> GetByCode(string code)
        {
            var permission = await _permissionService.GetPermissionByCodeAsync(code);
            if (permission == null)
            {
                return NotFound(new { message = "Permiso no encontrado" });
            }

            return Ok(permission);
        }

        /// <summary>
        /// Crea un nuevo permiso
        /// </summary>
        /// <param name="createPermissionDto">Datos del nuevo permiso</param>
        /// <returns>Permiso creado</returns>
        [HttpPost]
        [Authorize(Policy = "CanWritePermissions")]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PermissionDto>> Create([FromBody] CreatePermissionDto createPermissionDto)
        {
            var result = await _permissionService.CreatePermissionAsync(createPermissionDto);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Permission!.Id }, result.Permission);
        }

        /// <summary>
        /// Actualiza un permiso existente
        /// </summary>
        /// <param name="id">ID del permiso</param>
        /// <param name="updatePermissionDto">Datos actualizados del permiso</param>
        /// <returns>Permiso actualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanWritePermissions")]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PermissionDto>> Update(Guid id, [FromBody] UpdatePermissionDto updatePermissionDto)
        {
            var result = await _permissionService.UpdatePermissionAsync(id, updatePermissionDto);
            
            if (!result.Success)
            {
                if (result.Message.Contains("no encontrado"))
                {
                    return NotFound(new { message = result.Message });
                }
                return BadRequest(new { message = result.Message });
            }

            return Ok(result.Permission);
        }

        /// <summary>
        /// Elimina un permiso
        /// </summary>
        /// <param name="id">ID del permiso</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeletePermissions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _permissionService.DeletePermissionAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Permiso no encontrado" });
            }

            return NoContent();
        }
    }
}