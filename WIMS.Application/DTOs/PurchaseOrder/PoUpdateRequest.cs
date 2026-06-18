namespace WIMS.Application.DTOs.PurchaseOrder;

public class PoUpdateRequest
{
    public string? SupplierName { get; set; }
    public string? SupplierContact { get; set; }
    public DateOnly? ExpectedDelivery { get; set; }
    public string? Notes { get; set; }
}
