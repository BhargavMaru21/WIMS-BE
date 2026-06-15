using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class PurchaseOrder : AuditableEntity
{
    public required string PoNumber { get; set; }
    public required string SupplierName { get; set; }
    public string? SupplierContact { get; set; }
    public int WarehouseId { get; set; }
    public DateOnly? OrderDate { get; set; }
    public DateOnly ExpectedDelivery { get; set; }
    public PoStatus Status { get; set; } = PoStatus.Draft;
    public decimal TotalAmount { get; set; } = 0;
    public string? Notes { get; set; }
    public int? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Warehouse Warehouse { get; set; } = default!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = [];
    public User? SubmittedByUser { get; set; }
    public User? ApprovedByUser { get; set; }
    public User? RejectedByUser { get; set; }
    public User? CancelledByUser { get; set; }
}
