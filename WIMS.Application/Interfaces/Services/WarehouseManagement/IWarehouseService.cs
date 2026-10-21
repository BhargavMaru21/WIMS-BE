using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;

namespace WIMS.Application.Interfaces.Services.WarehouseManagement;

public interface IWarehouseService
{
    Task<ApiResponse<WarehouseResponse>> CreateWarehouse(WarehouseCreateRequest request);
    Task<ApiResponse<WarehouseResponse>> GetWarehouseById(int id);
    Task<ApiResponse<PagedResult<WarehouseResponse>>> GetWarehouses(QueryParameters qp);
    Task<ApiResponse<WarehouseResponse>> UpdateWarehouse(int id, WarehouseUpdateRequest request);
    Task<ApiResponse<string>> UpdateWarehouseStatus(int id,WarehouseStatusUpdateRequest request);
    Task<ApiResponse<string>> DeleteWarehouse(int id);
}
