using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class Product:SoftDeletableEntity
{
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int CategoryId { get; set; }
    public required int UomId { get; set; }
    public required decimal UnitPrice { get; set; }
    public required decimal ReorderLevel { get; set; } = 0;
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ProductCategory Category { get; set; } = default!;
    public UnitsOfMeasure Uom { get; set; } = default!;
}
