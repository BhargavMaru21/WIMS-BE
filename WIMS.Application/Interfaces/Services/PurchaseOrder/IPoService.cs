using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Profile;
using WIMS.Application.DTOs.PurchaseOrder;

namespace WIMS.Application.Interfaces.Services.PurchaseOrder;


public interface IPoService
{
    Task<ApiResponse<PoResponse>> CreatePo(PoCreateRequest request, int? warehouseId, int createdByUserId);
    Task<ApiResponse<PoResponse>> GetPoById(int id, int currentUserId, string currentUserRole, int? managerWarehouseId);
    Task<ApiResponse<PagedResult<PoResponse>>> GetPos(QueryParameters qp, int currentUserId, string currentUserRole, int? managerWarehouseId);
    Task<ApiResponse<PoResponse>> UpdatePo(int id, PoUpdateRequest request, int currentUserId, int? managerWarehouseId);
 
    Task<ApiResponse<PoItemResponse>> AddItem(int poId, PoItemCreateRequest request, int currentUserId, int? managerWarehouseId);
    Task<ApiResponse<PoItemResponse>> UpdateItem(int poId, int itemId, PoItemUpdateRequest request, int currentUserId, int? managerWarehouseId);
    Task<ApiResponse<string>> RemoveItem(int poId, int itemId, int currentUserId, int? managerWarehouseId);
 
    Task<ApiResponse<string>> SubmitPo(int id, int currentUserId, int? managerWarehouseId);
    Task<ApiResponse<string>> ApprovePo(int id, int currentUserId, string currentUserRole, int? managerWarehouseId);
    Task<ApiResponse<string>> RejectPo(int id, PoRejectRequest request, int currentUserId, string currentUserRole, int? managerWarehouseId);
    Task<ApiResponse<string>> CancelPo(int id, int currentUserId, int? managerWarehouseId);
}
