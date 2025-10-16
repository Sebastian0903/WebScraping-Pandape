using Domain.Entities;

namespace Domain.Repositories
{
    public interface ITodoRepository
    {
        Task<IEnumerable<Todo>> GetAllAsync();
        Task<Todo?> GetByIdAsync(int id);
        Task<Todo> AddAsync(Todo todo);
        Task UpdateAsync(Todo todo);
        Task DeleteAsync(int id);
    }
}