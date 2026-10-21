namespace WIMS.Application.DTOs.Warehouse;

public class ZoneDropdownResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int WarehouseId { get; set; }
}
