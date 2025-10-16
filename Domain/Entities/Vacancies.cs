namespace Domain.Entities
{
    public class Vacancies
    {
        // Identity
        public Guid Id { get; set; }

        // Properties
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; }
        public string Location { get; set; }

        // External System Integration
        public int ExternalId { get; set; }  // Antes: IdVacancy - Más claro

        // Audit (opcional pero recomendado)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Stages> Stages { get; set; } = new List<Stages>();
    }
}