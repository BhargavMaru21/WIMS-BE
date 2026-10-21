using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

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
