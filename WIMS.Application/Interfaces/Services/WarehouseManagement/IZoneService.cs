using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;

namespace WIMS.Application.Interfaces.Services.WarehouseManagement;

public interface IZoneService
{
    Task<ApiResponse<ZoneResponse>> CreateZone(ZoneCreateRequest request);
    Task<ApiResponse<ZoneResponse>> GetZoneById(int id);
    Task<ApiResponse<PagedResult<ZoneResponse>>> GetZones(QueryParameters qp);
    Task<ApiResponse<List<ZoneDropdownResponse>>> GetZonesDropdown(int? warehouseId = null);
    Task<ApiResponse<ZoneResponse>> UpdateZone(int id, ZoneUpdateRequest request);
    Task<ApiResponse<string>> UpdateZoneStatus(int id, ZoneStatusUpdateRequest request);
    Task<ApiResponse<string>> DeleteZone(int id);
}
