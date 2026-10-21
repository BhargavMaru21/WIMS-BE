using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;
using WIMS.Application.Interfaces.Services.WarehouseManagement;

namespace WIMS.Api.Controllers.WarehouseManagement;

[ApiController]
[Route("api/admin/bins")]
[Authorize(Policy = "AdminOnly")]
public class BinController : ControllerBase
{
    private readonly IBinService _binService;

    public BinController(IBinService binService)
    {
        _binService = binService;
    }


    [HttpPost]
    public async Task<ApiResponse<BinResponse>> CreateBin(BinCreateRequest request)
    {
        var response = await _binService.CreateBin(request);
        return response;
    }

    [HttpGet("{id:int}")]
    public async Task<ApiResponse<BinResponse>> GetBinById(int id)
    {
        var response = await _binService.GetBinById(id);
        return response;
    }

    [HttpDelete("{id:int}")]
    public async Task<ApiResponse<string>> DeleteBin(int id)
    {
        var response = await _binService.DeleteBin(id);
        return response;
    }

    [HttpGet]
    public async Task<IActionResult> GetBins([FromQuery] QueryParameters qp)
    {
        var response = await _binService.GetBins(qp);
        return Ok(response);
    }

    [HttpGet("all")]
    public async Task<ApiResponse<List<BinDropdownResponse>>> GetBinsDropdown(
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? zoneId = null)
    {
        var response = await _binService.GetBinsDropdown(warehouseId, zoneId);
        return response;
    }

    [HttpPatch("{id:int}")]
    public async Task<ApiResponse<BinResponse>> UpdateBin(int id, [FromBody] BinUpdateRequest request)
    {
        var response = await _binService.UpdateBin(id, request);
        return response;
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ApiResponse<string>> UpdateBinStatus(int id, [FromBody] BinStatusUpdateRequest request)
    {
        var response = await _binService.UpdateBinStatus(id, request);
        return response;
    }
}