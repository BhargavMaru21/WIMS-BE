namespace WIMS.Application.DTOs.GoodsReceipts;

public class PendingPoItemResponse
{
    public int PoItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal RemainingQty { get; set; }
}


