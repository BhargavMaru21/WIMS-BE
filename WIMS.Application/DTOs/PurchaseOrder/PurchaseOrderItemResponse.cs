namespace WIMS.Application.DTOs.PurchaseOrder;

public class PurchaseOrderItemResponse
{
    public int PoId { get; set; }
    public int ProductId { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
