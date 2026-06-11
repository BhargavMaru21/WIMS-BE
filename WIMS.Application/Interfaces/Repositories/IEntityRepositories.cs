using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetUserByRefreshTokenAsync(string token);
    Task<bool> IsEmailTakenAsync(string email, int? excludeUserId = null);
}

public interface IWarehouseRepository : IGenericRepository<Warehouse>
{
    Task<bool> HasStockAsync(int warehouseId);
}

public interface IZoneRepository : IGenericRepository<Zone>
{
    Task<List<Zone>> GetActiveZoneByWarehouseAsync(int warehouseId);
}

public interface IBinRepository : IGenericRepository<Bin>
{
    Task<List<Bin>> GetActiveBinByZoneAsync(int zoneId);
    Task<bool> HasStockAsync(int binId);
}

public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
}

public interface IUnitOfMeasureRepository : IGenericRepository<UnitsOfMeasure>
{
    Task<bool> IsAssignedToProductAsync(int uomId);
}

public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
{
    Task<bool> HasActiveProductsAsync(int categoryId);
    Task<bool> IsActive(int categoryId);
}

public interface IProductRepository : IGenericRepository<Product>
{
    Task<bool> HasStockAsync(int productId);
}