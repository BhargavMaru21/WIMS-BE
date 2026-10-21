using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Warehouse;

public class BinStatusUpdateRequest
{
    public EntityStatus Status { get; set; }
}
