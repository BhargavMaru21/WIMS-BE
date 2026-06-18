using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class UnitOfMeasureRepository : GenericRepository<UnitsOfMeasure>, IUnitOfMeasureRepository
{
    public UnitOfMeasureRepository(AppDbContext db) : base(db) { }

    public async Task<bool> IsAssignedToProductAsync(int uomId)
        => await _db.Set<Product>().AnyAsync(p => p.UomId == uomId);

}