namespace WIMS.Application.DTOs.Warehouse;

public class BinUpdateRequest
{
    public string? Name { get; set; }
    public decimal? MaxCapacity { get; set; }
}
