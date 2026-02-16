using System.Linq.Expressions;

namespace ISDSS.Application.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(int id);
    Task<List<T>> ListAsync(Expression<Func<T,bool>>? filter = null);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> SaveChangesAsync();
}
