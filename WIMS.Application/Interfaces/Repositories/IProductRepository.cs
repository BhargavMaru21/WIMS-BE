using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<bool> HasStockAsync(int productId);
}
