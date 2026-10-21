using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Warehouse;

public class BinCreateRequest
{
    public required int ZoneId { get; set; }
    public required string Name { get; set; }
    public decimal MaxCapacity { get; set; } = 0;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
