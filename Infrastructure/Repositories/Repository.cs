using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private DbSet<T> _dbSet;
        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }


        public async Task Add(T entity)
        {
            await _context.AddAsync(entity);
        }

        public async Task AddRangeR(IEnumerable<T> entities)
        {
            await _context.AddRangeAsync(entities);
        }

        public void Delete(T entity)
        {
            _context.Remove(entity);
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _context.RemoveRange(entities);
        }

        public async Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>>? predicate = null, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }



            return await query.ToListAsync();
        }

        public async Task<PaginatedResult<T>> GetAllPaginated(PaginationDTO dto, Expression<Func<T, bool>>? predicate = null, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            // Aplicar filtros primero
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (includeProperties != null)
            {
                foreach (var item in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }

            // Calcular total DESPUÉS de aplicar filtros
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / dto.pageSize);

            var paginatedData = await query
                .Skip((dto.numberPage - 1) * dto.pageSize)
                .Take(dto.pageSize)
                .ToListAsync();

            // Retornar PaginatedResult<T> en lugar de object
            return new PaginatedResult<T>
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = dto.numberPage,
                PageSize = dto.pageSize,
                Data = paginatedData
            };
        }

        public int GetAllCount(Expression<Func<T, bool>>? predicate = null, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query.Count();
        }

        public async Task<T> GetT(Expression<Func<T, bool>> predicate, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property);
                }
            }
            query = query.Where(predicate);
            return await query.FirstOrDefaultAsync();

        }

        public async Task<T> GetDefault()
        {
            IQueryable<T> query = _dbSet;
            return await query.FirstOrDefaultAsync();
        }


    }
}
