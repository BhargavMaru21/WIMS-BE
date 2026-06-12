namespace WIMS.Application.DTOs.PurchaseOrder;

public class PurchaseOrderItemCreateRequest
{
    public required int PoId { get; set; }
    public required int ProductId { get; set; }
    public required decimal OrderedQty { get; set; }
    public required decimal UnitPrice { get; set; }
}
