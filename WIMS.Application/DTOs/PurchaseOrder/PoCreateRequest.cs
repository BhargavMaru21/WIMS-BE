namespace WIMS.Application.DTOs.PurchaseOrder;

public class PoCreateRequest
{
    public required string SupplierName { get; set; }
    public string? SupplierContact { get; set; }
    public required DateOnly ExpectedDelivery { get; set; }
    public string? Notes { get; set; }
}