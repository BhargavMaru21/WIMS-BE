using WIMS.Application.DTOs;
using WIMS.Application.DTOs.GoodsReceipts;

namespace WIMS.Application.Interfaces.Services;

public interface IGrService
{
    Task<ApiResponse<GrResponse>> CreateGr(GrCreateRequest request);
    Task<ApiResponse<GrResponse>> GetGrById(int id);
    Task<ApiResponse<PagedResult<GrResponse>>> GetGrs(QueryParameters qp);
    Task<ApiResponse<List<PendingPoResponse>>> GetPendingPos();
}