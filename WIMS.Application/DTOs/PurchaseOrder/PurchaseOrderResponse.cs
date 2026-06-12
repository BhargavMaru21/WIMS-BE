namespace WIMS.Application.DTOs.PurchaseOrder;

public class PurchaseOrderResponse
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string? SupplierContact { get; set; }
    public int WarehouseId { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly ExpectedDelivery { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; } 
    public string? Notes { get; set; }
}
