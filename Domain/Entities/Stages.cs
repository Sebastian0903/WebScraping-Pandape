using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class Stages
    {
        // Identity
        public Guid Id { get; set; }

        // Properties
        public string Name { get; set; } = string.Empty;

        // Foreign Key
        public Guid VacancyId { get; set; }

        // External System Integration
        public int ExternalId { get; set; }  // Antes: IdVacancyStage - Más claro

        // Computed Properties (opcional)
        [JsonIgnore]
        public int CandidatesCount => Candidates?.Count ?? 0;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [JsonIgnore]
        public virtual Vacancies Vacancy { get; set; } = null!;

        [JsonIgnore]
        public virtual ICollection<PandapeCandidates> Candidates { get; set; } = new List<PandapeCandidates>();
    }
}