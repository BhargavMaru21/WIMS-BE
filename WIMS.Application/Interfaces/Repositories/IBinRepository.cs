using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IBinRepository : IGenericRepository<Bin>
{
    Task<List<Bin>> GetActiveBinByZoneAsync(int zoneId);
    Task<bool> HasStockAsync(int binId);
}
