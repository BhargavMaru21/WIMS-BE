using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.ProductCategory;

public class ProductCategoryStatusUpdateRequest
{
    public EntityStatus Status { get; set; }
}
