using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IWarehouseRepository : IGenericRepository<Warehouse>
{
    Task<bool> HasStockAsync(int warehouseId);
}
