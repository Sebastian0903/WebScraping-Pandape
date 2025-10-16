namespace Domain.Entities
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Guid PermissionId { get; set; }
        
        // Propiedades de navegación
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}