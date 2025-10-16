using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        public bool IsLdapUser { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class CreateUserDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public bool IsLdapUser { get; set; } = false;

        [Required]
        public int RoleId { get; set; } = 2; // Default to User role (assuming ID 2 is User)
    }

    public class UserPandapeDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
        public bool? IsActive { get; set; } = true;
    }

    public class UpdateUserDto
    {
        [StringLength(50, MinimumLength = 3)]
        public string? Username { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? DocumentNumber { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        public bool? IsActive { get; set; }
        
        public bool? IsLdapUser { get; set; }

        public int? RoleId { get; set; }
    }

    public class UserLoginDto
    {
        [Required]
        public string UsernameOrDocumentNumber { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class UserLoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public UserDto? User { get; set; }
    }
}