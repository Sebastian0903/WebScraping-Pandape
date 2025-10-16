using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Application.DTOs;
using Api.Services;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;

        public AuthController(IConfiguration configuration, UserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        /// <summary>
        /// Genera un token JWT para autenticación
        /// </summary>
        /// <param name="request">Credenciales de usuario</param>
        /// <returns>Token JWT</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(UserLoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserLoginResponseDto>> Login([FromBody] UserLoginDto request)
        {
            var result = await _userService.AuthenticateAsync(request);
            
            if (!result.Success)
            {
                return Unauthorized(new { message = result.Message });
            }

            var token = await GenerateJwtToken(result.User!.Username, result.User.RoleId);
            var expiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"] ?? "60"));

            return Ok(new UserLoginResponseDto
            {
                Token = token,
                Expiration = expiration,
                User = result.User
            });
        }

        /// <summary>
        /// Registra un nuevo usuario y genera un token JWT
        /// </summary>
        /// <param name="request">Datos del nuevo usuario</param>
        /// <returns>Token JWT</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserLoginResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserLoginResponseDto>> Register([FromBody] CreateUserDto request)
        {
            var result = await _userService.CreateUserAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            var token = await GenerateJwtToken(result.User!.Username, result.User.RoleId);
            var expiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"] ?? "60"));

            return StatusCode(StatusCodes.Status201Created, new UserLoginResponseDto
            {
                Token = token,
                Expiration = expiration,
                User = result.User
            });
        }

        /// <summary>
        /// Valida un token JWT
        /// </summary>
        /// <returns>Información del token si es válido</returns>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(typeof(TokenValidationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<TokenValidationResponse> ValidateToken()
        {
            // Si llegamos aquí, el token es válido (gracias al atributo [Authorize])
            var username = User.Identity?.Name;
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            
            return Ok(new TokenValidationResponse
            {
                IsValid = true,
                Username = username ?? string.Empty,
                Roles = roles
            });
        }
        
        /// <summary>
        /// Genera un token de prueba para desarrollo
        /// </summary>
        /// <returns>Token JWT para pruebas</returns>
        [HttpGet("dev-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserLoginResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserLoginResponseDto>> GetDevToken()
        {
            // Usuario de prueba para desarrollo
            var username = "admin";
            var roleId = 1; // Rol de administrador
            
            var token = await GenerateJwtToken(username, roleId);
            var expiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"] ?? "60"));
            
            return Ok(new UserLoginResponseDto
            {
                Token = token,
                Expiration = expiration,
                User = new UserDto 
                { 
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = "admin@example.com",
                    RoleId = roleId,
                    RoleName = "Administrator"
                }
            });
        }

        /// <summary>
        /// Renueva un token JWT existente
        /// </summary>
        /// <returns>Nuevo token JWT</returns>
        [HttpPost("refresh")]
        [Authorize]
        [ProducesResponseType(typeof(UserLoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserLoginResponseDto>> RefreshToken()
        {
            // Si llegamos aquí, el token es válido (gracias al atributo [Authorize])
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "User";
            
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            // Obtenemos la información actualizada del usuario
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return Unauthorized(new { message = "Usuario no encontrado" });
            }

            // Generamos un nuevo token
            var newToken = await GenerateJwtToken(username, user.RoleId);
            var expiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"] ?? "60"));

            return Ok(new UserLoginResponseDto
            {
                Token = newToken,
                Expiration = expiration,
                User = user
            });
        }

        private async Task<string> GenerateJwtToken(string username, int roleId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "dev-secret-key-secure-default-2024"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"] ?? "60"));

            // Obtener el usuario y su rol
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                throw new Exception("Usuario no encontrado");
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, user.RoleName ?? "User"),
                new Claim("roleId", roleId.ToString())
            };

            // Agregar permisos como claims basados en el rol
            var permissions = await _userService.GetPermissionsByRoleIdAsync(roleId);
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class TokenValidationResponse
    {
        public bool IsValid { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }
}