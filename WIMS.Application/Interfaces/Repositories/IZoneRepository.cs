using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;


public interface IZoneRepository : IGenericRepository<Zone>
{
    Task<List<Zone>> GetActiveZoneByWarehouseAsync(int warehouseId);
}