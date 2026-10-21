using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Domain.Entity;
using WIMS.Infrastructure.Data;

namespace WIMS.Infrastructure.Repository;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet
            .Include(u => u.Warehouse)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<bool> IsEmailTakenAsync(string email, int? excludeUserId = null)
        => await _dbSet.AnyAsync(u =>
            u.Email.ToLower() == email.ToLower() &&
            (excludeUserId == null || u.Id != excludeUserId));

    public async Task<User?> GetUserByRefreshTokenAsync(string token) => await _dbSet.FirstOrDefaultAsync(u => u.RefreshTokenHash == token);
}
