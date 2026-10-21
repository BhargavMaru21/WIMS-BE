using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Entity;

namespace WIMS.Infrastructure.Data.Configurations;

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> b)
    {
        b.ToTable("stock_transfers");

        b.Property(x => x.TransferNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.TransferNumber).IsUnique();

        b.HasOne(x => x.SourceWarehouse)
            .WithMany()
            .HasForeignKey(x => x.SourceWarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.DestWarehouse)
            .WithMany()
            .HasForeignKey(x => x.DestWarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.InitiatedByUser)
            .WithMany()
            .HasForeignKey(x => x.InitiatedBy)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockTransferItemConfiguration : IEntityTypeConfiguration<StockTransferItem>
{
    public void Configure(EntityTypeBuilder<StockTransferItem> b)
    {
        b.ToTable("stock_transfer_items");

        b.Property(x => x.Quantity).HasPrecision(12, 2).IsRequired();

        b.HasOne(x => x.StockTransfer)
            .WithMany(st => st.Items)
            .HasForeignKey(x => x.TransferId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SourceBin)
            .WithMany()
            .HasForeignKey(x => x.SourceBinId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.DestBin)
            .WithMany()
            .HasForeignKey(x => x.DestBinId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> b)
    {
        b.ToTable("stock_adjustments");

        b.Property(x => x.AdjustmentNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.AdjustmentNumber).IsUnique();
        b.Property(x => x.Quantity).HasPrecision(12, 2).IsRequired();
        b.Property(x => x.ReasonNotes).HasMaxLength(500);
        b.Property(x => x.RejectionReason).HasMaxLength(500);

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Bin)
            .WithMany()
            .HasForeignKey(x => x.BinId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .IsRequired(false)       
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedBy)
            .IsRequired(false)       
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReorderAlertConfiguration : IEntityTypeConfiguration<ReorderAlert>
{
    public void Configure(EntityTypeBuilder<ReorderAlert> b)
    {
        b.ToTable("reorder_alerts");

        b.Property(x => x.CurrentStock).HasPrecision(12, 2).IsRequired();
        b.Property(x => x.ReorderLevel).HasPrecision(12, 2).IsRequired();

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AcknowledgedByUser)
            .WithMany()
            .HasForeignKey(x => x.AcknowledgedBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");

        b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(20).IsRequired();
        b.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
        b.Property(x => x.PerformedAt).IsRequired();

        b.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}