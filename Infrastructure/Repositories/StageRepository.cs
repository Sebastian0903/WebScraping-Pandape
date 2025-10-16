using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class StageRepository : Repository<Stages>, IStageRepository
    {
        private readonly AppDbContext _context;

        public StageRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Stages?> GetByVacancyAndExternalId(Guid vacancyId, int externalStageId)
        {
            return await _context.Stages
                .FirstOrDefaultAsync(s => s.VacancyId == vacancyId &&
                                       s.ExternalId == externalStageId);
        }

        public async Task<Stages?> GetByExternalId(int externalStageId)
        {
            return await _context.Stages
                .FirstOrDefaultAsync(s => s.ExternalId == externalStageId);
        }

        public async Task<List<Stages>> GetStagesByVacancyId(Guid vacancyId)
        {
            return await _context.Stages
                .Where(s => s.VacancyId == vacancyId)
                .ToListAsync();
        }

        //public async Task<List<Stages>> GetStagesByVacancyExternalId(int externalVacancyId)
        //{
        //    return await _context.Stages
        //        .Where(s => s.VacancyExternalId == externalVacancyId)
        //        .ToListAsync();
        //}

        public void Update(Stages stage)
        {
            var stageDb = _context.Stages.FirstOrDefault(x => x.Id == stage.Id);
            if (stageDb != null)
            {
                stageDb.Name = stage.Name;
                stageDb.ExternalId = stage.ExternalId;
                stageDb.VacancyId = stage.VacancyId;
                //stageDb.VacancyExternalId = stage.VacancyExternalId;
            }
            _context.Stages.Update(stageDb);
        }
    }
}