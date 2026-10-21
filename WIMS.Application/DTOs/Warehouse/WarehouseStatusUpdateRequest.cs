using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Warehouse;

public class WarehouseStatusUpdateRequest
{
    public EntityStatus Status { get; set; }
}
