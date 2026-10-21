using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class Bin : SoftDeletableEntity
{
public required int ZoneId { get; set; }
public required string Code { get; set; }
public required string Name { get; set; }
public required decimal MaxCapacity { get; set; }
public EntityStatus Status { get; set; } = EntityStatus.Active;
public Zone Zone { get; set; } = default!;
}
