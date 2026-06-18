using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.PurchaseOrder;

public class PoStatusUpdateRequest
{
    public required PoStatus Status {get; set;}
    public string? RejectionReason {get; set;}
}
