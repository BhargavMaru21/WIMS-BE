using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Warehouse;

public class WarehouseCreateRequest
{
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string ContactPerson { get; set; }
    public required string ContactPhone { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
