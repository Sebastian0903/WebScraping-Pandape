using Domain.Entities;

namespace Domain.Repositories
{
    public interface IVacancyRepository : IRepository<Vacancies>
    {
        Task<Vacancies?> GetByExternalId(int externalId);
        Task<Vacancies?> GetByIdWithStages(Guid id);
        Task<List<Vacancies>> GetActiveVacancies();
        void Update(Vacancies vacancy);
    }
}