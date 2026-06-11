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

    private int GetCurrentUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> CreateZone(ZoneCreateRequest request)
    {
        var response = await _zoneService.CreateZone(request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);

            return BadRequest(response);
        }
           
        return StatusCode(201, response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetZoneById(int id)
    {
        var response = await _zoneService.GetZoneById(id);

        if (!response.IsSuccess)
            return NotFound(response);

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var response = await _zoneService.DeleteZone(id,GetCurrentUserId());

        if (!response.IsSuccess)
            return NotFound(response);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetZones([FromQuery] QueryParameters qp)
    {
        var response = await _zoneService.GetZones(qp);
        return Ok(response);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetZonesDropdown([FromQuery] int? warehouseId = null)
    {
        var response = await _zoneService.GetZonesDropdown(warehouseId);
        return Ok(response);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateZone(int id, ZoneUpdateRequest request)
    {
        var response = await _zoneService.UpdateZone(id, request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);

            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateZoneStatus(int id, ZoneStatusUpdateRequest request)
    {
        var response = await _zoneService.UpdateZoneStatus(id, request, GetCurrentUserId());

        if (!response.IsSuccess){
            if(response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }
}