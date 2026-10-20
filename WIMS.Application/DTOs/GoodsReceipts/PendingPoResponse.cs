namespace WIMS.Application.DTOs.GoodsReceipts;

public class PendingPoResponse
{
    public int PoId { get; set; }
    public string PoNumber { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public DateOnly ExpectedDelivery { get; set; }
    public string Status { get; set; } = null!;
    public List<PendingPoItemResponse> Items { get; set; } = [];
}
