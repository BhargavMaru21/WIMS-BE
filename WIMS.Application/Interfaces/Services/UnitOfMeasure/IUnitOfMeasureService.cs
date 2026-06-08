using WIMS.Application.DTOs;
using WIMS.Application.DTOs.UnitOfMeasure;

namespace WIMS.Application.Interfaces.Services.UnitOfMeasure;

public interface IUnitOfMeasureService
{
    Task<ApiResponse<UnitResponse>> CreateUnit (CreateUnitRequest request , int createdByUserId);
    Task<ApiResponse<List<UnitResponse>>> GetUnitsDropdown ();
    // Task<ApiResponse<PagedResult<UnitResponse>>> GetUnits (QueryParameters qp);
    // Task<ApiResponse<string>> DeleteUnit (int unitId , int deletedByUserId);
    // Task<ApiResponse<UnitResponse>> UpdateUnit(UpdateUnitRequest request, int modifiedByUserId);
}
