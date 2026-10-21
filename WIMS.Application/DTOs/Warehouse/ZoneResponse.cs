namespace WIMS.Application.DTOs.Warehouse;

public class ZoneResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string Status { get; set; } = null!;
}
