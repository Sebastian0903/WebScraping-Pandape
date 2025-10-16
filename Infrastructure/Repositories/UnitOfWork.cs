using Domain.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IPermissionRepository PermissionRepo { get; private set; }
        public IRoleRepository RoleRepo { get; private set; }
        public ITodoRepository TodoRepo { get; private set; }
        public IUserPandapeRepository UserPandapesRepo { get; private set; }
        public IUserRepository UserRepo { get; private set; }
        public IPandapeCandidatesRepository CandidateRepo { get; private set; }
        public IVacancyRepository VacancyRepo { get; private set; }
        public IStageRepository StageRepo { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            PermissionRepo = new PermissionRepository(context);
            RoleRepo = new RoleRepository(context);
            TodoRepo = new TodoRepository(context);
            UserPandapesRepo = new UserPandapeRepository(context);
            UserRepo = new UserRepository(context);
            CandidateRepo = new PandapeCandidatesRepository(context);
            VacancyRepo = new VacancyRepository(_context);
            StageRepo = new StageRepository(_context);

        }

        public async Task Save()
        {
            await _context.SaveChangesAsync();
        }
    }
}
