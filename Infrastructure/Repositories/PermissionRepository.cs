using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly AppDbContext _context;

        public PermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetAllAsync()
        {
            return await _context.Permissions.ToListAsync();
        }

        public async Task<Permission?> GetByIdAsync(Guid id)
        {
            return await _context.Permissions.FindAsync(id);
        }

        public async Task<Permission?> GetByCodeAsync(string code)
        {
            return await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower());
        }

        public async Task<Permission> CreateAsync(Permission permission)
        {
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return permission;
        }

        public async Task<Permission> UpdateAsync(Permission permission)
        {
            _context.Entry(permission).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return permission;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return false;
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return true;
        }


        
        public async Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<Guid> ids)
        {
            return await _context.Permissions
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();
        }
    }
}