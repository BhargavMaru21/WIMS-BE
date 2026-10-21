namespace WIMS.Application.DTOs.Warehouse;

public class BinDropdownResponse
{
     public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int ZoneId { get; set; }
    public int WarehouseId { get; set; }
}
