using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Products;

public class ProductCreateRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int CategoryId { get; set; }
    public required int UomId { get; set; }
    public required decimal UnitPrice { get; set; }
    public decimal ReorderLevel { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}

