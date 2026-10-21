using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

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
