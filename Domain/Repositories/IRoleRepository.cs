using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string name);
        Task<Role> CreateAsync(Role role);
        Task<Role> UpdateAsync(Role role);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(int roleId);
    }
}