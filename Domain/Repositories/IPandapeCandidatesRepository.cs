using Domain.Entities;

namespace Domain.Repositories
{
    public interface IPandapeCandidatesRepository : IRepository<PandapeCandidates>
    {
        void Update(PandapeCandidates userPandape);
    }
}