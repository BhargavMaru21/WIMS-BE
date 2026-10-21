using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Entity;

namespace WIMS.Infrastructure.Data.Configurations;


public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");

        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.Role).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.RefreshTokenHash).HasMaxLength(500);
        b.Property(x => x.PasswordResetTokenHash).HasMaxLength(500);
        b.HasOne(x => x.Warehouse)
            .WithMany(w => w.Users)
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("warehouses");

        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Address).HasMaxLength(300).IsRequired();
        b.Property(x => x.City).HasMaxLength(100).IsRequired();
        b.Property(x => x.ContactPerson).HasMaxLength(150).IsRequired();
        b.Property(x => x.ContactPhone).HasMaxLength(30).IsRequired();
    }
}

public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> b)
    {
        b.ToTable("zones");

        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();

        b.HasOne(x => x.Warehouse)
            .WithMany(w => w.Zones)
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BinConfiguration : IEntityTypeConfiguration<Bin>
{
    public void Configure(EntityTypeBuilder<Bin> b)
    {
        b.ToTable("bins");

        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.MaxCapacity).HasPrecision(12, 2);

        b.HasOne(x => x.Zone)
            .WithMany(z => z.Bins)
            .HasForeignKey(x => x.ZoneId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
