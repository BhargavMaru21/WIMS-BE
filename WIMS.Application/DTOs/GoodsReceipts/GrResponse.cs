namespace WIMS.Application.DTOs.GoodsReceipts;

public class GrResponse
{
    public int Id { get; set; }
    public string GrnNumber { get; set; } = null!;
    public int PoId { get; set; }
    public string PoNumber { get; set; } = null!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public DateOnly ReceiptDate { get; set; }
    public int ReceivedBy { get; set; }
    public string ReceivedByName { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<GrItemResponse> Items { get; set; } = [];
}


