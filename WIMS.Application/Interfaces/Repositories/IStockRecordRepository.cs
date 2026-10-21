using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IStockRecordRepository : IGenericRepository<StockRecord>
{
    Task<StockRecord> GetOrCreateAsync(int productId, int warehouseId, int binId);
}
