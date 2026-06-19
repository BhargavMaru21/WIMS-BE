using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class StockMovementRepository : GenericRepository<StockMovement>,IStockMovementRepository
{
    public StockMovementRepository(AppDbContext db) : base(db) {}
}
