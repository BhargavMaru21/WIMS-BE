using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet
            .Include(u => u.Warehouse)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<bool> IsEmailTakenAsync(string email, int? excludeUserId = null)
        => await _dbSet.AnyAsync(u =>
            u.Email.ToLower() == email.ToLower() &&
            (excludeUserId == null || u.Id != excludeUserId));

    public async Task<User?> GetUserByRefreshTokenAsync(string token) => await _dbSet.FirstOrDefaultAsync(u => u.RefreshTokenHash == token);
}

public class WarehouseRepository : GenericRepository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(AppDbContext db) : base(db) { }

    public async Task<bool> HasStockAsync(int warehouseId)
        => await _db.Set<StockRecord>().AnyAsync(sr => sr.WarehouseId == warehouseId && sr.Quantity > 0);
}

public class ZoneRepository : GenericRepository<Zone>, IZoneRepository
{
    public ZoneRepository(AppDbContext db) : base(db) { }

    public async Task<List<Zone>> GetActiveZoneByWarehouseAsync(int warehouseId)
            => await _dbSet
                .AsNoTracking()
                .Where(z => z.WarehouseId == warehouseId && z.Status == EntityStatus.Active)
                .OrderBy(z => z.Code)
                .ToListAsync();
}

public class BinRepository : GenericRepository<Bin>, IBinRepository
{
    public BinRepository(AppDbContext db) : base(db) { }

    public async Task<List<Bin>> GetActiveBinByZoneAsync(int zoneId)
         => await _dbSet
             .AsNoTracking()
             .Where(b => b.ZoneId == zoneId && b.Status == EntityStatus.Active)
             .OrderBy(b => b.Code)
             .ToListAsync();

    public async Task<bool> HasStockAsync(int binId)
        => await _db.Set<StockRecord>()
            .AnyAsync(sr => sr.BinId == binId && sr.Quantity > 0);
}
public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext db) : base(db) { }

}
public class UnitOfMeasureRepository : GenericRepository<UnitsOfMeasure>, IUnitOfMeasureRepository
{
    public UnitOfMeasureRepository(AppDbContext db) : base(db) { }

    public async Task<bool> IsAssignedToProductAsync(int uomId)
        => await _db.Set<Product>().AnyAsync(p => p.UomId == uomId);

}

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

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }

    public async Task<bool> HasStockAsync(int productId)
        => await _db.Set<StockRecord>()
            .AnyAsync(sr => sr.ProductId == productId && sr.Quantity > 0);
}

public class PurchaseOrderRepository : GenericRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(AppDbContext db) : base(db) {}

}