namespace WIMS.Application.DTOs.Warehouse;

public class BinResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = null!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public decimal MaxCapacity { get; set; }
    public string Status { get; set; } = null!;
}
