using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class UserPandapeRepository : Repository<UserPandape>, IUserPandapeRepository
    {
        private readonly AppDbContext _context;
        public UserPandapeRepository(AppDbContext context): base(context) 
        {
            _context = context;
        }

        public void Update(UserPandape userPandape)
        {
            var userDb = _context.UsersPandape.FirstOrDefault(x=>x.Id == userPandape.Id);
            if (userDb != null)
            {
                userDb.Email = userPandape.Email;
                userDb.PasswordHash = userPandape.PasswordHash;
            }
            _context.UsersPandape.Update(userDb);
        }
    }
}