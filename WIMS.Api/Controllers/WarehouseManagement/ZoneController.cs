using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;
using WIMS.Application.Interfaces.Services.WarehouseManagement;

namespace WIMS.Api.Controllers.WarehouseManagement;

[ApiController]
[Route("api/admin/zones")]
[Authorize(Policy = "AdminOnly")]
public class ZoneController : ControllerBase
{
    private readonly IZoneService _zoneService;

    public ZoneController(
        IZoneService zoneService)
    {
        _zoneService = zoneService;
    }

    [HttpPost]
    public async Task<ApiResponse<ZoneResponse>> CreateZone(ZoneCreateRequest request)
    {
        var response = await _zoneService.CreateZone(request);
        return response ;
    }

    [HttpGet("{id:int}")]
    public async Task<ApiResponse<ZoneResponse>> GetZoneById(int id)
    {
        var response = await _zoneService.GetZoneById(id);
        return response;
    }

    [HttpDelete("{id:int}")]
    public async Task<ApiResponse<string>> DeleteZone(int id)
    {
        var response = await _zoneService.DeleteZone(id);
        return response;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<ZoneResponse>>> GetZones([FromQuery] QueryParameters qp)
    {
        var response = await _zoneService.GetZones(qp);
        return response;
    }

    [HttpGet("all")]
    public async Task<ApiResponse<List<ZoneDropdownResponse>>> GetZonesDropdown([FromQuery] int? warehouseId = null)
    {
        var response = await _zoneService.GetZonesDropdown(warehouseId);
        return response;
    }

    [HttpPatch("{id:int}")]
    public async Task<ApiResponse<ZoneResponse>> UpdateZone(int id, ZoneUpdateRequest request)
    {
        var response = await _zoneService.UpdateZone(id, request);
        return response;
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ApiResponse<string>> UpdateZoneStatus(int id, ZoneStatusUpdateRequest request)
    {
        var response = await _zoneService.UpdateZoneStatus(id, request);
        return response;
    }
}