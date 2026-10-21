using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class WarehouseRepository : GenericRepository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(AppDbContext db) : base(db) { }

    public async Task<bool> HasStockAsync(int warehouseId)
        => await _db.Set<StockRecord>().AnyAsync(sr => sr.WarehouseId == warehouseId && sr.Quantity > 0);
}
