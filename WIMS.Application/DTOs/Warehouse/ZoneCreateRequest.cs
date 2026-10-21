using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Warehouse;

public class ZoneCreateRequest
{
    public required int WarehouseId { get; set; }
    public required string Name { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
