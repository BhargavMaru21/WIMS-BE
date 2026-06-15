namespace WIMS.Application.DTOs.PurchaseOrder;

public class PoItemUpdateRequest
{
    public decimal? OrderedQty { get; set; }
    public decimal? UnitPrice { get; set; }
}
