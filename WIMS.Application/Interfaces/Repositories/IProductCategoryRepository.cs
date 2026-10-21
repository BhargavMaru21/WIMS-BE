using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
{
    Task<bool> HasActiveProductsAsync(int categoryId);
    Task<bool> IsActive(int categoryId);
}
