using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }

    public async Task<bool> HasStockAsync(int productId)
        => await _db.Set<StockRecord>()
            .AnyAsync(sr => sr.ProductId == productId && sr.Quantity > 0);
}
