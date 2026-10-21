using WIMS.Domain.Entity;

namespace WIMS.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetUserByRefreshTokenAsync(string token);
    Task<bool> IsEmailTakenAsync(string email, int? excludeUserId = null);
}
