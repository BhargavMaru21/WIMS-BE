using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class ProductCategory:SoftDeletableEntity
{
public required string Name { get; set; }
public string? Description { get; set; }
public EntityStatus Status { get; set; } = EntityStatus.Active;
}
