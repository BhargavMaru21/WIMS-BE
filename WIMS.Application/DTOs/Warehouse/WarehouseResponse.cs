namespace WIMS.Application.DTOs.Warehouse;

public class WarehouseResponse
{
    public int Id  {get; set;}
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string ContactPerson { get; set; } = null!;
    public string ContactPhone { get; set; } = null!;
    public string Status { get; set; } = null!;
}
