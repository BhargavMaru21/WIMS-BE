using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class Zone : SoftDeletableEntity
{
    public required int WarehouseId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public Warehouse Warehouse { get; set; } = default!;
    public ICollection<Bin> Bins { get; set; } = new List<Bin>();
}
