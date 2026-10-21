using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Profile;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Domain.Enums;

namespace WIMS.Application.Interfaces.Services.PurchaseOrder;


public interface IPoService
{
    Task<ApiResponse<PoResponse>> CreatePo(PoCreateRequest request);
    Task<ApiResponse<PoResponse>> GetPoById(int id);
    Task<ApiResponse<PagedResult<PoResponse>>> GetPos(QueryParameters qp);
    Task<ApiResponse<PoResponse>> UpdatePo(int id, PoUpdateRequest request);
 
    Task<ApiResponse<PoItemResponse>> AddItem(int poId, PoItemCreateRequest request);
    Task<ApiResponse<PoItemResponse>> UpdateItem(int poId, int itemId, PoItemUpdateRequest request);
    Task<ApiResponse<string>> RemoveItem(int poId, int itemId);
    Task<ApiResponse<string>>  UpdateStatus(int id, PoStatusUpdateRequest request);
}
