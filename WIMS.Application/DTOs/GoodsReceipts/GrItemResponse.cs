namespace WIMS.Application.DTOs.GoodsReceipts;

public class GrItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int PoItemId { get; set; }
    public int BinId { get; set; }
    public string BinName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string Condition { get; set; } = null!;
}
