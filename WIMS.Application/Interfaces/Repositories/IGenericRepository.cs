using System.Linq.Expressions;
using WIMS.Application.DTOs;
using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class, IEntity
{
    Task<List<T>> GetAllAsync(
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? includes = null
    );

    Task<T?> GetAsync(
        Expression<Func<T, bool>> filter,
        bool useNoTracking = true,
        Func<IQueryable<T>, IQueryable<T>>? includes = null
    );

    Task<PagedResult<T>> GetPaginatedAsync(
        QueryParameters qp,
        string[]? searchableColumns = null,
        Func<IQueryable<T>, IQueryable<T>>? includes = null
    );

    Task<T> CreateAsync(T entity);
    Task<T> AddAsync(T entity);                  
    Task StageUpdateAsync(T entity);       
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(T entity);
    Task<bool> SoftDeleteAsync(T entity, int deletedBy);
    Task<bool> SaveChangesAsync();

    //helper
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    IQueryable<T> GetQueryable();

    
    //transaction
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task CreateSavepointAsync(string savepointName);
    Task RollbackToSavepointAsync(string savepointName);
    Task ReleaseSavepointAsync(string savepointName);
}
