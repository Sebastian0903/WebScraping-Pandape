using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Api.Services
{
    public class UserService
    {
        //private readonly IUserRepository  _unitOfWork.RoleRepo;
        //private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly LdapService _ldapService;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, LdapService ldapService, ILogger<UserService> logger, IUnitOfWork unitOfWork)
        {
            // _unitOfWork.RoleRepo = userRepository;
            //_roleRepository = roleRepository;
            _ldapService = ldapService;
            _logger = logger;
            _unitOfWork = unitOfWork;

        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.UserRepo.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _unitOfWork.UserRepo.GetByIdAsync(id);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _unitOfWork.UserRepo.GetByUsernameAsync(username);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<(bool Success, string Message, UserDto? User)> CreateUserAsync(CreateUserDto createUserDto)
        {
            // Verificar si el usuario ya existe
            var existingUser = await _unitOfWork.UserRepo.GetByUsernameAsync(createUserDto.Username);
            if (existingUser != null)
            {
                return (false, "El nombre de usuario ya está en uso", null);
            }

            // Verificar si el email ya existe
            existingUser = await _unitOfWork.UserRepo.GetByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                return (false, "El correo electrónico ya está en uso", null);
            }

            // Crear hash de la contraseña
            var (passwordHash, salt) = HashPassword(createUserDto.Password);

            // Validar rol
            var role = await _unitOfWork.RoleRepo.GetByIdAsync(createUserDto.RoleId);
            if (role == null)
            {
                return (false, "Rol no válido", null);
            }

            // Crear el usuario
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                DocumentNumber = createUserDto.DocumentNumber,
                PasswordHash = passwordHash,
                Salt = salt,
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.UserRepo.AddAsync(user);
            return (true, "Usuario creado exitosamente", MapToDto(user));
        }

        public async Task<(bool Success, string Message, UserDto? User)> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
        {
            var user = await _unitOfWork.UserRepo.GetByIdAsync(id);
            if (user == null)
            {
                return (false, "Usuario no encontrado", null);
            }

            // Actualizar nombre de usuario si se proporciona
            if (!string.IsNullOrEmpty(updateUserDto.Username) && updateUserDto.Username != user.Username)
            {
                var existingUser = await _unitOfWork.UserRepo.GetByUsernameAsync(updateUserDto.Username);
                if (existingUser != null)
                {
                    return (false, "El nombre de usuario ya está en uso", null);
                }
                user.Username = updateUserDto.Username;
            }

            // Actualizar email si se proporciona
            if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
            {
                var existingUser = await _unitOfWork.UserRepo.GetByEmailAsync(updateUserDto.Email);
                if (existingUser != null)
                {
                    return (false, "El correo electrónico ya está en uso", null);
                }
                user.Email = updateUserDto.Email;
            }
            
            // Actualizar número de documento si se proporciona
            if (!string.IsNullOrEmpty(updateUserDto.DocumentNumber) && updateUserDto.DocumentNumber != user.DocumentNumber)
            {
                user.DocumentNumber = updateUserDto.DocumentNumber;
            }

            // Actualizar contraseña si se proporciona
            if (!string.IsNullOrEmpty(updateUserDto.Password))
            {
                var (passwordHash, salt) = HashPassword(updateUserDto.Password);
                user.PasswordHash = passwordHash;
                user.Salt = salt;
            }

            // Actualizar estado activo si se proporciona
            if (updateUserDto.IsActive.HasValue)
            {
                user.IsActive = updateUserDto.IsActive.Value;
            }
            
            // Actualizar estado de usuario LDAP si se proporciona
            if (updateUserDto.IsLdapUser.HasValue)
            {
                user.IsLdapUser = updateUserDto.IsLdapUser.Value;
            }

            // Actualizar rol si se proporciona
            if (updateUserDto.RoleId.HasValue)
            {
                var role = await _unitOfWork.RoleRepo.GetByIdAsync(updateUserDto.RoleId.Value);
                if (role == null)
                {
                    return (false, "Rol no válido", null);
                }
                user.RoleId = role.Id;
            }

            await _unitOfWork.UserRepo.UpdateAsync(user);
            return (true, "Usuario actualizado exitosamente", MapToDto(user));
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _unitOfWork.UserRepo.GetByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            await _unitOfWork.UserRepo.DeleteAsync(id);
            return true;
        }



        public async Task<(bool Success, string Message, List<UserPandape>? Users)> GetListUserPandapeAsync()
        {

            // Verificar si el email ya existe
            //var existingUser = await _unitOfWork.UserPandapesRepo.GetAll();
            List<UserPandape> users = (List<UserPandape>)await _unitOfWork.UserPandapesRepo.GetAll();
            if (users.Count == 0)
            {
                return (false, "El correo electrónico ya está en uso", null);
            }

            return (true, "Usuario creado exitosamente", users);
        }
        public async Task<(bool Success, string Message, UserPandape? User)> CreateUserPandapeAsync(UserPandapeDto createUserDto)
        {

            // Verificar si el email ya existe
            var existingUser = await _unitOfWork.UserPandapesRepo.GetAll(x =>x.Email == createUserDto.Email);
            if (existingUser == null) 
            {
                return (false, "El correo electrónico ya está en uso", null);
            }
            var (passwordHash, salt) = HashPassword(createUserDto.Password);
            var userToCreate = new UserPandape()
            {
                Id = Guid.NewGuid(),
                Email = createUserDto.Email,
                PasswordHash = passwordHash,
                IsActive = true
            };
            await _unitOfWork.UserPandapesRepo.Add(userToCreate);
            await _unitOfWork.Save();
            return (true, "Usuario creado exitosamente", MapToPandapeDto(userToCreate));
        }

        public async Task<(bool Success, string Message, UserPandape? User)> UpdateUserPandapeAsync(Guid id, UserPandapeDto updateUserDto)
        {
            try
            {
                // Buscar el usuario existente
                var user = await _unitOfWork.UserPandapesRepo.GetT(x => x.Id == id);
                if (user == null)
                {
                    return (false, "Usuario no encontrado", null);
                }

                // Validar y actualizar email si se proporciona y es diferente
                if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
                {
                    // Verificar si el nuevo email ya existe en otro usuario
                    var existingUser = await _unitOfWork.UserPandapesRepo.GetT(x => x.Email == updateUserDto.Email && x.Id != id);
                    if (existingUser != null)
                    {
                        return (false, "El correo electrónico ya está en uso", null);
                    }

                    user.Email = updateUserDto.Email;
                }

                // Actualizar otras propiedades según sea necesario
                if (!string.IsNullOrEmpty(updateUserDto.Password))
                {
                    var (passwordHash, salt) = HashPassword(updateUserDto.Password);
                    user.PasswordHash = passwordHash;
                }

                // Marcar como modificado y guardar
                _unitOfWork.UserPandapesRepo.Update(user);
                await _unitOfWork.Save();

                return (true, "Usuario actualizado exitosamente", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario con ID: {UserId}", id);
                return (false, "Error interno al actualizar el usuario", null);
            }
        }

        //public async Task<bool> DeleteUserPandapeAsync(Guid id)
        //{
        //    var user = await  _unitOfWork.UserPandapesRepo.GetByIdAsync(id);
        //    if (user == null)
        //    {
        //        return false;
        //    }

        //    await  _unitOfWork.UserPandapeRepo.DeleteAsync(id);
        //    return true;
        //}



        public async Task<(bool Success, string Message, UserDto? User)> AuthenticateAsync(UserLoginDto loginDto)
        {
            // Intentar buscar por nombre de usuario
            var user = await  _unitOfWork.UserRepo.GetByUsernameAsync(loginDto.UsernameOrDocumentNumber);
            
            // Si no se encuentra, intentar buscar por número de documento
            if (user == null)
            {
                user = await  _unitOfWork.UserRepo.GetByDocumentNumberAsync(loginDto.UsernameOrDocumentNumber);
            }
            
            // Si el usuario existe en la base de datos local
            if (user != null)
            {
                if (!user.IsActive)
                {
                    return (false, "La cuenta está desactivada", null);
                }

                // Si es un usuario LDAP, verificar siempre su estado en LDAP antes de permitir el acceso
                if (user.IsLdapUser)
                {
                    try
                    {
                        var ldapResult = await _ldapService.AuthenticateAsync(loginDto.UsernameOrDocumentNumber, loginDto.Password);
                        
                        if (!ldapResult.Success)
                        {
                            // Si el usuario está desactivado en LDAP o hay otro problema, rechazar el acceso
                            string errorMessage = "Autenticación LDAP fallida";
                            
                            if (ldapResult.Message == "DISABLEDACCOUNT")
                            {
                                // Actualizar el estado local para reflejar que está desactivado en LDAP
                                user.IsActive = false;
                                await  _unitOfWork.UserRepo.UpdateAsync(user);
                                errorMessage = "La cuenta ha sido desactivada en el directorio LDAP";
                            }
                            
                            return (false, errorMessage, null);
                        }
                        
                        // Autenticación LDAP exitosa, actualizar último login
                        user.LastLogin = DateTime.UtcNow;
                        await  _unitOfWork.UserRepo.UpdateAsync(user);
                        return (true, "Autenticación LDAP exitosa", MapToDto(user));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al verificar estado LDAP para usuario {Username}", user.Username);
                        return (false, "Error al verificar estado en LDAP", null);
                    }
                }

                // Para usuarios no-LDAP, verificar contraseña local
                var passwordHash = HashPasswordWithSalt(loginDto.Password, user.Salt);
                if (passwordHash == user.PasswordHash)
                {
                    // Autenticación local exitosa
                    // Actualizar último login
                    user.LastLogin = DateTime.UtcNow;
                    await  _unitOfWork.UserRepo.UpdateAsync(user);
                    return (true, "Autenticación exitosa", MapToDto(user));
                }
            }
            
            // Si la autenticación local falla o el usuario no existe, intentar con LDAP
            try
            {
                var ldapResult = await _ldapService.AuthenticateAsync(loginDto.UsernameOrDocumentNumber, loginDto.Password);
                
                if (ldapResult.Success)
                {
                    // Autenticación LDAP exitosa
                    if (user == null)
                    {
                        // El usuario no existe en la base de datos local, crearlo
                        var (passwordHash, salt) = HashPassword(loginDto.Password);
                        
                        // Crear un nuevo usuario con la información disponible
                        user = new User
                        {
                            Id = Guid.NewGuid(),
                            Username = loginDto.UsernameOrDocumentNumber,
                            DocumentNumber = loginDto.UsernameOrDocumentNumber, // Asumimos que podría ser el número de documento
                            Email = $"{loginDto.UsernameOrDocumentNumber}@example.com", // Email temporal
                            PasswordHash = passwordHash,
                            Salt = salt,
                            RoleId = 2, // Rol por defecto (asumimos que 2 es el rol de usuario)
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true,
                            LastLogin = DateTime.UtcNow,
                            IsLdapUser = true // Marcar como usuario de LDAP
                        };
                        
                        await  _unitOfWork.UserRepo.AddAsync(user);
                        _logger.LogInformation($"Usuario creado automáticamente desde LDAP: {loginDto.UsernameOrDocumentNumber}");
                    }
                    else
                    {
                        // El usuario existe, actualizar último login
                        user.LastLogin = DateTime.UtcNow;
                        await  _unitOfWork.UserRepo.UpdateAsync(user);
                    }
                    
                    return (true, "Autenticación LDAP exitosa", MapToDto(user));
                }
                else
                {
                    // Autenticación LDAP fallida
                    string errorMessage = "Usuario o contraseña incorrectos";
                    
                    switch (ldapResult.Message)
                    {
                        case "DISABLEDACCOUNT":
                            errorMessage = "La cuenta LDAP está desactivada";
                            // Si el usuario existe en la base de datos local, actualizar su estado
                            if (user != null)
                            {
                                user.IsActive = false;
                                await  _unitOfWork.UserRepo.UpdateAsync(user);
                                _logger.LogInformation($"Usuario {user.Username} desactivado localmente debido a desactivación en LDAP");
                            }
                            break;
                        case "NOTAMEMBER":
                            errorMessage = "El usuario no pertenece al grupo requerido";
                            break;
                        case "NOTFOUND":
                            errorMessage = "Usuario no encontrado en el directorio LDAP";
                            break;
                    }
                    
                    _logger.LogWarning($"Autenticación LDAP fallida para {loginDto.UsernameOrDocumentNumber}: {ldapResult.Message}");
                    return (false, errorMessage, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en la autenticación LDAP para {loginDto.UsernameOrDocumentNumber}");
                return (false, "Error en la autenticación", null);
            }
        }

        // Métodos privados para mapeo y hash de contraseñas
        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DocumentNumber = user.DocumentNumber,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                IsActive = user.IsActive,
                IsLdapUser = user.IsLdapUser,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name
            };
        }

        private UserPandape MapToPandapeDto(UserPandape user)
        {
            return new UserPandape
            {
                Id = user.Id,
                Email = user.Email,
                IsActive = user.IsActive
            };
        }

        // Obtener permisos basados en rol
        public async Task<List<string>> GetPermissionsByRoleIdAsync(int roleId)
        {
            var permissions = await _unitOfWork.RoleRepo.GetPermissionsByRoleIdAsync(roleId);
            return permissions.Select(p => p.Code).ToList();
        }
        
        public List<string> GetPermissionsByRole(string roleName)
        {
            var role = _unitOfWork.RoleRepo.GetByNameAsync(roleName).Result;
            if (role == null)
            {
                return new List<string>();
            }
            
            return GetPermissionsByRoleIdAsync(role.Id).Result;
        }
        
        public async Task<List<string>> GetPermissionsByRoleAsync(string roleName)
        {
            var role = await _unitOfWork.RoleRepo.GetByNameAsync(roleName);
            if (role == null)
            {
                return new List<string>();
            }
            
            return await GetPermissionsByRoleIdAsync(role.Id);
        }

        private (string PasswordHash, string Salt) HashPassword(string password)
        {
            // Generar salt aleatorio
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Generar hash con el salt
            string passwordHash = HashPasswordWithSalt(password, salt);

            return (passwordHash, salt);
        }

        private string HashPasswordWithSalt(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Combinar password y salt
            byte[] combinedBytes = new byte[passwordBytes.Length + saltBytes.Length];
            Buffer.BlockCopy(passwordBytes, 0, combinedBytes, 0, passwordBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, combinedBytes, passwordBytes.Length, saltBytes.Length);

            // Calcular hash
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}