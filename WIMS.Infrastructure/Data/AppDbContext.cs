using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WIMS.Domain.Entity;

namespace WIMS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Bin> Bins => Set<Bin>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<UnitsOfMeasure> UnitsOfMeasure => Set<UnitsOfMeasure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockRecord> StockRecords => Set<StockRecord>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<GoodsDispatch> GoodsDispatches => Set<GoodsDispatch>();
    public DbSet<GoodsDispatchItem> GoodsDispatchItems => Set<GoodsDispatchItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<ReorderAlert> ReorderAlerts => Set<ReorderAlert>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Apply all IEntityTypeConfiguration<T> classes from Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    var converterType = typeof(EnumToStringConverter<>)
                        .MakeGenericType(property.ClrType);

                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    property.SetValueConverter(converter);
                }
            }
            // Auto soft-delete filter 
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var param = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(
                    Expression.Equal(prop, Expression.Constant(false)),
                    param
                );
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

}