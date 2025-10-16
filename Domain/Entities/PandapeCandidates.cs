using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class PandapeCandidates
    {
        // Identity
        public Guid Id { get; set; }

        // Basic Info
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Media
        public string CvUrl { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;

        // Status
        public bool IsActive { get; set; } = true;

        // Foreign Key
        public Guid StageId { get; set; }

        // External System Integration Fields
        public int ExternalVacancyId { get; set; }      // Antes: IdVacancy
        public int ExternalStageId { get; set; }        // Antes: IdVacancyFolder
        public string ExternalMatchId { get; set; } = string.Empty;  // Antes: IdMatch

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Stages Stage { get; set; } = null!;

        // Computed Properties (acceso fácil)
        [NotMapped]
        public Vacancies Vacancy => Stage?.Vacancy;

        [NotMapped]
        public string VacancyName => Stage?.Vacancy?.Name ?? string.Empty;
    }
}