using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class PandapeCandidatesRepository : Repository<PandapeCandidates>, IPandapeCandidatesRepository
    {
        private readonly AppDbContext _context;
        public PandapeCandidatesRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(PandapeCandidates pandapeCandidates)
        {
            var userDb = _context.PandapeCandidates.FirstOrDefault(x => x.Id == pandapeCandidates.Id);
            if (userDb != null)
            {
                userDb.Username = pandapeCandidates.Username;
                userDb.CvUrl = pandapeCandidates.CvUrl;
                userDb.IsActive = pandapeCandidates.IsActive;
                userDb.PhoneNumber = pandapeCandidates.PhoneNumber;
                userDb.ProfileImageUrl = pandapeCandidates.ProfileImageUrl;
                userDb.ExternalVacancyId = pandapeCandidates.ExternalVacancyId;
                userDb.ExternalStageId = pandapeCandidates.ExternalStageId;
            }
            _context.PandapeCandidates.Update(userDb);
        }
    }
}
