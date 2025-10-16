using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Relación con permisos
        public virtual ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
        
        // Relación con usuarios
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}