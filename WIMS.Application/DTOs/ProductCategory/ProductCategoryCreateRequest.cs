using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.ProductCategory;

public class ProductCategoryCreateRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
