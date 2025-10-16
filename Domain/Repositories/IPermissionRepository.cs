using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IPermissionRepository
    {
        Task<IEnumerable<Permission>> GetAllAsync();
        Task<Permission?> GetByIdAsync(Guid id);
        Task<Permission?> GetByCodeAsync(string code);
        Task<Permission> CreateAsync(Permission permission);
        Task<Permission> UpdateAsync(Permission permission);
        Task<bool> DeleteAsync(Guid id);

        Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<Guid> ids);
    }
}