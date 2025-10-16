using Domain.Entities;

namespace Domain.Repositories
{
    public interface IStageRepository : IRepository<Stages>
    {
        Task<Stages?> GetByVacancyAndExternalId(Guid vacancyId, int externalStageId);
        Task<Stages?> GetByExternalId(int externalStageId);
        Task<List<Stages>> GetStagesByVacancyId(Guid vacancyId);
        //Task<List<Stages>> GetStagesByVacancyExternalId(int externalVacancyId);
        void Update(Stages stage);
    }
}