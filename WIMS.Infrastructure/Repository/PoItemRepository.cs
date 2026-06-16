using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class PoItemRepository : GenericRepository<PurchaseOrderItem>, IPoItemRepository
{
    public PoItemRepository(AppDbContext db) : base(db) { }
}