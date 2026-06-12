using WIMS.Application.DTOs;
using WIMS.Application.DTOs.PurchaseOrder;

namespace WIMS.Application.Interfaces.Services.PurchaseOrder;

public interface IPurchaseOrderService
{
    Task<ApiResponse<PurchaseOrderResponse>> CreatePurchaseOrder (PurchaseOrderCreateRequest request, int createdByUserId,int warehouseId);
}
