using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;

namespace WIMS.Application.Interfaces.Services.WarehouseManagement;

public interface IBinService
{
    Task<ApiResponse<BinResponse>> CreateBin(BinCreateRequest request);
    Task<ApiResponse<BinResponse>> GetBinById(int id);
    Task<ApiResponse<PagedResult<BinResponse>>> GetBins(QueryParameters qp);
    Task<ApiResponse<List<BinDropdownResponse>>> GetBinsDropdown(int? warehouseId = null, int? zoneId = null);
    Task<ApiResponse<BinResponse>> UpdateBin(int id, BinUpdateRequest request);
    Task<ApiResponse<string>> UpdateBinStatus(int id, BinStatusUpdateRequest request);
    Task<ApiResponse<string>> DeleteBin(int id);
}
