using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class VacancyRepository : Repository<Vacancies>, IVacancyRepository
    {
        private readonly AppDbContext _context;

        public VacancyRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Vacancies?> GetByExternalId(int externalId)
        {
            return await _context.Vacancies
                .FirstOrDefaultAsync(v => v.ExternalId == externalId && v.IsActive);
        }

        public async Task<Vacancies?> GetByIdWithStages(Guid id)
        {
            return await _context.Vacancies.FirstOrDefaultAsync(v => v.Id == id && v.IsActive);
        }

        public async Task<List<Vacancies>> GetActiveVacancies()
        {
            return await _context.Vacancies
                .Where(v => v.IsActive)
                .OrderBy(v => v.Name)
                .ToListAsync();
        }

        public void Update(Vacancies vacancy)
        {
            var vacancyDb = _context.Vacancies.FirstOrDefault(x => x.Id == vacancy.Id);
            if (vacancyDb != null)
            {
                vacancyDb.Name = vacancy.Name;
                //vacancyDb.Description = vacancy.Description;
                vacancyDb.ExternalId = vacancy.ExternalId;
                vacancyDb.IsActive = vacancy.IsActive;
                // No actualizar CreatedAt
            }
            _context.Vacancies.Update(vacancyDb);
        }
    }
}