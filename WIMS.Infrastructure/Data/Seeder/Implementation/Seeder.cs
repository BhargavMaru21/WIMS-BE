using Microsoft.EntityFrameworkCore;
using WIMS.Application.Interfaces.Common;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using WIMS.Infrastructure.Data.Seeder.Interface;

namespace WIMS.Infrastructure.Data.Seeder.Implementation;

public class Seeder : ISeeder
{

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public Seeder(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }


    public async Task SeedAsync()
    {
        Console.WriteLine("Seeding data...");
      bool adminexists = await _context.Users.AnyAsync(u => u.Role == UserRole.Administrator);

        if (!adminexists)
        {
            var adminUser = new User
            {
                FullName = "admin",
                Email = "plutus1920@gmail.com",
                PasswordHash =  _passwordHasher.Hash("Admin@123"),
                Role =UserRole.Administrator,
                Status = UserStatus.Active
            };
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();
        }
    }

}
