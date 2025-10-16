using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePermissionDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
    }

    public class UpdatePermissionDto
    {
        [StringLength(50, MinimumLength = 3)]
        public string? Name { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string? Code { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }
    }
}