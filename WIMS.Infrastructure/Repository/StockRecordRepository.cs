using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class StockRecordRepository : GenericRepository<StockRecord>, IStockRecordRepository
{
    public StockRecordRepository(AppDbContext db) : base(db) { }

    public async Task<StockRecord> GetOrCreateAsync(int productId, int warehouseId, int binId)
    {
        var record = await _dbSet.FirstOrDefaultAsync(x =>
            x.ProductId == productId &&
            x.WarehouseId == warehouseId &&
            x.BinId == binId);

        if (record is not null) return record;

        record = new StockRecord
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            BinId = binId,
            Quantity = 0
        };

        await _dbSet.AddAsync(record);
        return record;
    }
}
