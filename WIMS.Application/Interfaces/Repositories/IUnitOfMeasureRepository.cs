using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IUnitOfMeasureRepository : IGenericRepository<UnitsOfMeasure>
{
    Task<bool> IsAssignedToProductAsync(int uomId);
}
