using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Permission
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Relación con roles
        public virtual ICollection<RolePermission> Roles { get; set; } = new List<RolePermission>();
    }
}