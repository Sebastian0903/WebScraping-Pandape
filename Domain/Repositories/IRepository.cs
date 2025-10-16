using System.Linq.Expressions;

namespace Domain.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task Add(T entity);
        Task AddRangeR(IEnumerable<T> entities);
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);
        Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>>? predicate = null, string? includeProperties = null);
        int GetAllCount(Expression<Func<T, bool>>? predicate = null, string? includeProperties = null);
        // Asegúrate de que PaginationDTO esté definido y referenciado correctamente
        Task<PaginatedResult<T>> GetAllPaginated(PaginationDTO pagination, Expression<Func<T, bool>>? predicate = null, string? includeProperties = null);
        Task<T> GetT(Expression<Func<T, bool>> predicate, string? includeProperties = null);
        Task<T> GetDefault();
    }

    public class PaginationDTO
    {
        public int numberPage { get; set; }
        public int pageSize { get; set; }
        public string? category { get; set; }
        public string? asigned { get; set; }
        public string? textSearch { get; set; }
    }

    public class PaginatedResult<T>
    {
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
    }
}