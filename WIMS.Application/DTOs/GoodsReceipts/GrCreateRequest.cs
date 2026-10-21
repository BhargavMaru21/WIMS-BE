namespace WIMS.Application.DTOs.GoodsReceipts;

public class GrCreateRequest
{
    public required int PoId { get; set; }
    public string? Notes { get; set; }
    public required List<GrItemCreateRequest> Items { get; set; }
}

