namespace WIMS.Application.DTOs.PurchaseOrder;

public class PoItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal OrderedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal LineTotal { get; set; }
}
