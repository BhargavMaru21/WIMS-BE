using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Warehouse;

public class ZoneStatusUpdateRequest
{
    public EntityStatus Status { get; set; }
}
