using Application.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Domain.Entities;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Obtiene todos los usuarios
        /// </summary>
        /// <returns>Lista de usuarios</returns>
        [HttpGet]
        [Authorize(Policy = "CanReadUsers")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <returns>Usuario</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanReadUsers")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return Ok(user);
        }

        /// <summary>
        /// Obtiene el perfil del usuario actual
        /// </summary>
        /// <returns>Usuario</returns>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return Ok(user);
        }

        /// <summary>
        /// Actualiza un usuario
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <param name="updateUserDto">Datos del usuario</param>
        /// <returns>Usuario actualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanWriteUsers")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto updateUserDto)
        {
            var result = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!result.Success)
            {
                if (result.Message.Contains("no encontrado"))
                {
                    return NotFound(new { message = result.Message });
                }
                return BadRequest(new { message = result.Message });
            }

            return Ok(result.User);
        }

        /// <summary>
        /// Actualiza el perfil del usuario actual
        /// </summary>
        /// <param name="updateUserDto">Datos del usuario</param>
        /// <returns>Usuario actualizado</returns>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserDto updateUserDto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // No permitir cambiar el rol desde esta ruta
            updateUserDto.RoleId = null;

            var result = await _userService.UpdateUserAsync(user.Id, updateUserDto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result.User);
        }

        /// <summary>
        /// Elimina un usuario
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteUsers")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var success = await _userService.DeleteUserAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return NoContent();
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        /// <param name="createUserDto">Datos del usuario a crear</param>
        /// <returns>Usuario creado</returns>
        [HttpPost]
        [Authorize(Policy = "CanWriteUsers")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createUserDto)
        {
            var result = await _userService.CreateUserAsync(createUserDto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.User.Id }, result.User);
        }


        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        /// <param name="createUserDto">Datos del usuario a crear</param>
        /// <returns>Usuario creado</returns>
        [HttpPost("createUserPandape")]
        [Authorize(Policy = "CanWriteUsers")]
        [ProducesResponseType(typeof(UserPandapeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserPandapeDto>> CreatePandapeUser([FromBody] UserPandapeDto createUserDto)
        {
            var result = await _userService.CreateUserPandapeAsync(createUserDto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.User.Id }, result.User);
        }


        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        /// <param name="createUserDto">Datos del usuario que se va a actualizar</param>
        /// <returns>Usuario actualizado</returns>
        [HttpPut("updateUserPandape/{id}")]
        [Authorize(Policy = "CanWriteUsers")]
        [ProducesResponseType(typeof(UserPandapeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserPandapeDto>> UpdatePandapeUser(Guid id,[FromBody] UserPandapeDto createUserDto)
        {
            var result = await _userService.UpdateUserPandapeAsync(id,createUserDto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.User.Email }, result.Message);
        }


        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        /// <param name="createUserDto">Datos del usuario que se va a actualizar</param>
        /// <returns>Usuario actualizado</returns>
        [HttpPut("getUsersPandape/{id}")]
        [Authorize(Policy = "CanReadUsers")]
        [ProducesResponseType(typeof(UserPandapeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserPandapeDto>> GetListPandapeUser()
        {
            var result = await _userService.GetListUserPandapeAsync();
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Users }, result.Message);
        }
    }
}