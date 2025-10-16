namespace Domain.Repositories
{
    public interface IUnitOfWork
    {
        public IPermissionRepository PermissionRepo { get; }
        public IRoleRepository RoleRepo { get; }
        public ITodoRepository TodoRepo { get; }
        public IUserPandapeRepository UserPandapesRepo { get; }
        public IUserRepository UserRepo { get; }
        public IVacancyRepository VacancyRepo { get; }
        public IStageRepository StageRepo { get; }
        public IPandapeCandidatesRepository CandidateRepo { get; }

        Task Save();
    }
}
