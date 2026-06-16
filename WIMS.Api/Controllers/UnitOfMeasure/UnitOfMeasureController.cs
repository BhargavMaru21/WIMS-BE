using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.UnitOfMeasure;
using WIMS.Application.Interfaces.Services.UnitOfMeasure;

namespace WIMS.Api.Controllers.UnitOfMeasureController;

[ApiController]
[Route("api/unitOfMeasure")]
[Authorize(Policy = "AdminOnly")]
public class UnitOfMeasureController : ControllerBase
{
    private readonly IUnitOfMeasureService _unitOfMeasureService;
    public UnitOfMeasureController(IUnitOfMeasureService unitOfMeasureService)
    {
        _unitOfMeasureService = unitOfMeasureService;
    }

    [HttpPost]
    public async Task<ApiResponse<UnitResponse>> CreateUnit(CreateUnitRequest request)
    {
        var response = await _unitOfMeasureService.CreateUnit(request);
        return response;
    }
    [HttpGet]
    public async Task<ApiResponse<List<UnitResponse>>> GetUnits()
    {
        var response = await _unitOfMeasureService.GetUnitsDropdown();
        return response;
    }

    [HttpPatch("{id:int}")]
    public async Task<ApiResponse<UnitResponse>> UpdateUom(int id, UpdateUnitRequest request)
    {
        var response = await _unitOfMeasureService.UpdateUnit(id, request);
        return response;
    }

    [HttpDelete("{id:int}")]
    public async Task<ApiResponse<string>> DeleteUom(int id)
    {
        var response = await _unitOfMeasureService.DeleteUnit(id);
        return response;
    }
}
