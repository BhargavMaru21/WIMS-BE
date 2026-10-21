namespace WIMS.Application.DTOs.Warehouse;

public class WarehouseUpdateRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
}
