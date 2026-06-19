using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.GoodsReceipts;

public class GrItemCreateRequest
{
    public required int PoItemId { get; set; }
    public required int ProductId { get; set; }
    public required int BinId { get; set; }
    public required decimal Quantity { get; set; }
    public required ReceiptCondition Condition { get; set; }
}
