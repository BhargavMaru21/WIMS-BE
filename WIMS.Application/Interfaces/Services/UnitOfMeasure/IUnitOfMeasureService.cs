using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Products;
using WIMS.Application.DTOs.UnitOfMeasure;

namespace WIMS.Application.Interfaces.Services.UnitOfMeasure;

public interface IUnitOfMeasureService
{
    Task<ApiResponse<UnitResponse>> CreateUnit (CreateUnitRequest request , int createdByUserId);
    Task<ApiResponse<List<UnitResponse>>> GetUnitsDropdown ();
    Task<ApiResponse<UnitResponse>> UpdateUnit(int id, UpdateUnitRequest request, int modifiedByUserId);
}
