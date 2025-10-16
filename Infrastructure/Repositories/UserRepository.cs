using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetByDocumentNumberAsync(string documentNumber)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.DocumentNumber == documentNumber);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string usernameOrDocumentNumber, string passwordHash)
        {
            // Intentar buscar por nombre de usuario
            var user = await GetByUsernameAsync(usernameOrDocumentNumber);
            
            // Si no se encuentra, intentar buscar por número de documento
            if (user == null)
            {
                user = await GetByDocumentNumberAsync(usernameOrDocumentNumber);
            }
            
            if (user == null) return false;
            
            return user.PasswordHash == passwordHash && user.IsActive;
        }
    }
}