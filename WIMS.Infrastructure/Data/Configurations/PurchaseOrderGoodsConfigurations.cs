using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Entity;

namespace WIMS.Infrastructure.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("purchase_orders");

        b.Property(x => x.PoNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.PoNumber).IsUnique();
        b.Property(x => x.SupplierName).HasMaxLength(200).IsRequired();
        b.Property(x => x.SupplierContact).HasMaxLength(20);
        b.Property(x => x.TotalAmount).HasPrecision(16, 2).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.RejectionReason).HasMaxLength(500);

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
            
         b.HasOne(x => x.SubmittedByUser)
            .WithMany()
            .HasForeignKey(x => x.SubmittedBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
 
        b.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
 
        b.HasOne(x => x.RejectedByUser)
            .WithMany()
            .HasForeignKey(x => x.RejectedBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
 
        b.HasOne(x => x.CancelledByUser)
            .WithMany()
            .HasForeignKey(x => x.CancelledBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
 
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> b)
    {
        b.ToTable("purchase_order_items");

        b.HasIndex(x => new { x.PoId, x.ProductId }).IsUnique();
        b.Property(x => x.OrderedQty).HasPrecision(12, 2).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(14, 2).IsRequired();
        b.Property(x => x.ReceivedQty).HasPrecision(12, 2).IsRequired();
        b.Property(x => x.LineTotal).HasPrecision(16, 2).IsRequired();

        b.HasOne(x => x.PurchaseOrder)
            .WithMany(po => po.Items)
            .HasForeignKey(x => x.PoId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> b)
    {
        b.ToTable("goods_receipts");

        b.Property(x => x.GrnNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.GrnNumber).IsUnique();
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasOne(x => x.PurchaseOrder)
            .WithMany(po => po.GoodsReceipts)
            .HasForeignKey(x => x.PoId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReceivedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReceivedBy)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptItem> b)
    {
        b.ToTable("goods_receipt_items");

        b.Property(x => x.Quantity).HasPrecision(12, 2).IsRequired();

        b.HasOne(x => x.GoodsReceipt)
            .WithMany(gr => gr.Items)
            .HasForeignKey(x => x.GrnId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.PoItem)
            .WithMany()
            .HasForeignKey(x => x.PoItemId)
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
    }
}

public class GoodsDispatchConfiguration : IEntityTypeConfiguration<GoodsDispatch>
{
    public void Configure(EntityTypeBuilder<GoodsDispatch> b)
    {
        b.ToTable("goods_dispatches");

        b.Property(x => x.GdnNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.GdnNumber).IsUnique();
        b.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        b.Property(x => x.CustomerAddress).HasMaxLength(500);

        b.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GoodsDispatchItemConfiguration : IEntityTypeConfiguration<GoodsDispatchItem>
{
    public void Configure(EntityTypeBuilder<GoodsDispatchItem> b)
    {
        b.ToTable("goods_dispatch_items");

        b.Property(x => x.Quantity).HasPrecision(12, 2).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(14, 2).IsRequired();
        b.Property(x => x.LineTotal).HasPrecision(16, 2).IsRequired();

        b.HasOne(x => x.GoodsDispatch)
            .WithMany(gd => gd.Items)
            .HasForeignKey(x => x.GdnId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

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
    }
}