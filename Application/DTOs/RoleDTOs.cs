using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }

    public class CreateRoleDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }

    public class UpdateRoleDto
    {
        [StringLength(50, MinimumLength = 3)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public List<Guid>? PermissionIds { get; set; }
    }
}