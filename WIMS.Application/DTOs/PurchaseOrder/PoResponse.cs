namespace WIMS.Application.DTOs.PurchaseOrder;

public class PoResponse
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string? SupplierContact { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public DateOnly? OrderDate { get; set; }
    public DateOnly ExpectedDelivery { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
 
    public int? SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime? SubmittedAt { get; set; }
 
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
 
    public int? RejectedBy { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
 
    public int? CancelledBy { get; set; }
    public string? CancelledByName { get; set; }
    public DateTime? CancelledAt { get; set; }

    public int CreatedBy {get; set;}
    public DateTime CreatedAt {get; set;}
    
    public bool CanApprove { get; set; }
    public bool CanEdit { get; set; }
 
    public List<PoItemResponse> Items { get; set; } = [];
}
 

