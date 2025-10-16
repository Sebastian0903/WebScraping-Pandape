using Domain.Entities;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByDocumentNumberAsync(string documentNumber);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
        Task<bool> ValidateCredentialsAsync(string usernameOrDocumentNumber, string passwordHash);
    }
}