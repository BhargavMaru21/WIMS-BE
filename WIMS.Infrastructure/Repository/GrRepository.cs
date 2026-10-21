using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class GrRepository : GenericRepository<GoodsReceipt> , IGrRepository
{
    public GrRepository (AppDbContext db) : base(db) {}
}
