using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class ProductCategoryRepository : GenericRepository<ProductCategory>, IProductCategoryRepository
{
    public ProductCategoryRepository(AppDbContext db) : base(db) { }

    public async Task<bool> HasActiveProductsAsync(int categoryId)
        => await _db.Set<Product>().AnyAsync(p => p.CategoryId == categoryId && p.Status == EntityStatus.Active);

    public async Task<bool> IsActive(int categoryId)
        => await _dbSet.Where(p => p.Id == categoryId)
            .Select(p => p.Status == EntityStatus.Active)
            .FirstOrDefaultAsync();
}
