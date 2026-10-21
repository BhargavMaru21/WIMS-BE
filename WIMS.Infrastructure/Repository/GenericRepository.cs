namespace WIMS.Infrastructure.Repository;

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WIMS.Application.DTOs;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntity
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _dbSet;
    private IDbContextTransaction? _transaction;

    public GenericRepository(AppDbContext db)
    {
        _db = db;
        _dbSet = db.Set<T>();
    }
    public async Task<List<T>> GetAllAsync(
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (includes is not null)
            query = includes(query);

        query = orderBy is not null ? orderBy(query) : query.OrderBy(x => x.Id);

        return await query.ToListAsync();
    }

    public async Task<T?> GetAsync(
        Expression<Func<T, bool>> filter,
        bool useNoTracking = true,
        Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        IQueryable<T> query = _dbSet;

        if (useNoTracking)
            query = query.AsNoTracking();

        if (includes is not null)
            query = includes(query);

        return await query.FirstOrDefaultAsync(filter);
    }

    public async Task<PagedResult<T>> GetPaginatedAsync(
        QueryParameters qp,
        string[]? searchableColumns = null,
        Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (includes is not null)
            query = includes(query);

        foreach (var filter in qp.Filters)
        {
            var prop = typeof(T).GetProperty(
                filter.Key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is null) continue;

            var param = Expression.Parameter(typeof(T), "x");
            var propExpr = Expression.Property(param, prop);
            Expression? condition = null;

            if (prop.PropertyType == typeof(string))
            {
                condition = Expression.Equal(propExpr, Expression.Constant(filter.Value));
            }
            else if (prop.PropertyType == typeof(int))
            {
                var values = filter.Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToArray();

                if (values.Length > 1)
                {
                    var ids = values.Select(int.Parse).ToList();

                    condition = Expression.Call(
                        Expression.Constant(ids),
                        typeof(List<int>).GetMethod(nameof(List<int>.Contains), new[] { typeof(int) })!,
                        propExpr);
                }
                else if (int.TryParse(filter.Value, out var intVal))
                {
                    condition = Expression.Equal(
                        propExpr,
                        Expression.Constant(intVal));
                }
            }
            else if (prop.PropertyType == typeof(int?))
            {
                if (int.TryParse(filter.Value, out var intVal))
                    condition = Expression.Equal(propExpr, Expression.Constant((int?)intVal, typeof(int?)));
            }
            else if (prop.PropertyType == typeof(bool))
            {
                if (bool.TryParse(filter.Value, out var boolVal))
                    condition = Expression.Equal(propExpr, Expression.Constant(boolVal));
            }
            else if (prop.PropertyType.IsEnum)
            {
                var values = filter.Value
                 .Split(',', StringSplitOptions.RemoveEmptyEntries)
                 .Select(v => v.Trim())
                 .ToList();

                if (values.Count > 1)
                {
                    var enumList = new List<object>();
                    foreach (var v in values)
                    {
                        try { enumList.Add(Enum.Parse(prop.PropertyType, v, ignoreCase: true)); }
                        catch { }
                    }

                    if (enumList.Count > 0)
                    {
                        var typedArray = Array.CreateInstance(prop.PropertyType, enumList.Count);
                        for (int i = 0; i < enumList.Count; i++)
                            typedArray.SetValue(enumList[i], i);

                        var containsMethod = typeof(Enumerable)
                            .GetMethods()
                            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                            .MakeGenericMethod(prop.PropertyType);

                        condition = Expression.Call(
                            containsMethod,
                            Expression.Constant(typedArray),
                            propExpr);
                    }
                }
                else
                {
                    try
                    {
                        var enumVal = Enum.Parse(prop.PropertyType, filter.Value, ignoreCase: true);
                        condition = Expression.Equal(propExpr, Expression.Constant(enumVal, prop.PropertyType));
                    }
                    catch { }
                }
            }

            if (condition is not null)
                query = query.Where(Expression.Lambda<Func<T, bool>>(condition, param));
        }

        //  Search 
        if (!string.IsNullOrWhiteSpace(qp.Search)
            && searchableColumns is { Length: > 0 })
        {
            var term = $"%{qp.Search.Trim()}%";
            var param = Expression.Parameter(typeof(T), "x");
            Expression? combined = null;

            foreach (var col in searchableColumns)
            {
                var prop = typeof(T).GetProperty(
                    col, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                // Only string properties can be searched
                if (prop is null || prop.PropertyType != typeof(string)) continue;

                var propExpr = Expression.Property(param, prop);

                // EF.Functions.ILike(column, "%term%")  — PostgreSQL only, case-insensitive
                var iLike = typeof(NpgsqlDbFunctionsExtensions)
                    .GetMethod("ILike", new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

                var call = Expression.Call(
                    iLike,
                    Expression.Constant(EF.Functions),
                    propExpr,
                    Expression.Constant(term));

                combined = combined is null ? call : Expression.OrElse(combined, call);
            }

            if (combined is not null)
                query = query.Where(Expression.Lambda<Func<T, bool>>(combined, param));
        }

        //Total Count 
        var totalCount = await query.CountAsync();

        //Sorting 
        if (!string.IsNullOrWhiteSpace(qp.SortBy))
        {
            var prop = typeof(T).GetProperty(
                qp.SortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is not null)
            {
                var param = Expression.Parameter(typeof(T), "x");
                var keySelector = Expression.Lambda(Expression.Property(param, prop), param);
                var methodName = qp.SortDirection == "desc" ? "OrderByDescending" : "OrderBy";

                var ordered = typeof(Queryable)
                    .GetMethods()
                    .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), prop.PropertyType)
                    .Invoke(null, new object[] { query, keySelector });

                query = (IQueryable<T>)ordered!;
            }
        }
        else
        {
            // Default: newest first
            var hasCreatedAt = typeof(T).GetProperty("CreatedAt") is not null;
            if (hasCreatedAt)
            {
                var param = Expression.Parameter(typeof(T), "x");
                var key = Expression.Lambda<Func<T, DateTime>>(
                    Expression.Property(param, "CreatedAt"), param);
                query = query.OrderByDescending(key);
            }
            else
            {
                query = query.OrderByDescending(x => x.Id);
            }
        }

        //Pagination
        var items = await query
            .Skip((qp.PageNumber - 1) * qp.PageSize)
            .Take(qp.PageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = qp.PageNumber,
            PageSize = qp.PageSize
        };
    }

    public async Task<T> CreateAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public Task StageUpdateAsync(T entity)
    {
        _db.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        _db.Update(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteAsync(T entity, int deletedBy)
    {
        if (entity is ISoftDelete softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedBy = deletedBy;
            softDeletable.DeletedOn = DateTime.UtcNow;
            _db.Update(entity);
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
        return true;
    }


    //Helper

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter)
        => await _dbSet.AnyAsync(filter);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        => filter is null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(filter);

    public IQueryable<T> GetQueryable()
        => _dbSet.AsQueryable();

    public async Task BeginTransactionAsync()
    {
        _transaction = await _db.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task CreateSavepointAsync(string savepointName)
    {
        if (_transaction is not null)
            await _transaction.CreateSavepointAsync(savepointName);
    }

    public async Task RollbackToSavepointAsync(string savepointName)
    {
        if (_transaction is not null)
            await _transaction.RollbackToSavepointAsync(savepointName);
    }

    public async Task ReleaseSavepointAsync(string savepointName)
    {
        if (_transaction is not null)
            await _transaction.ReleaseSavepointAsync(savepointName);
    }
}