using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class GrItemRepository : GenericRepository<GoodsReceiptItem> , IGrItemRepository
{
    public GrItemRepository (AppDbContext db) : base(db) {}
}
