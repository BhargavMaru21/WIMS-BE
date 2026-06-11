using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Products;

public class ProductStatusUpdateRequest
{
     public EntityStatus Status { get; set; }
}
