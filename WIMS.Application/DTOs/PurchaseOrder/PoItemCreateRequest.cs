namespace WIMS.Application.DTOs.PurchaseOrder;

public class PoItemCreateRequest
{
    public required int ProductId { get; set; }
    public required decimal OrderedQty { get; set; }
}
