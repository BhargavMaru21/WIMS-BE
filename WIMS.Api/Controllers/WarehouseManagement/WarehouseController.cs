using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;
using WIMS.Application.Interfaces.Services.WarehouseManagement;

namespace WIMS.Api.Controllers.WarehouseManagement;

[ApiController]
[Route("api/admin/warehouses")]
[Authorize(Policy = "AdminOnly")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    private int GetCurrentUserId()
    => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> CreateWarehouse(WarehouseCreateRequest request)
    {
        var response = await _warehouseService.CreateWarehouse(request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            return BadRequest(response);
        }

        return StatusCode(201, response);
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetWarehouseById(int id)
    {
        var response = await _warehouseService.GetWarehouseById(id);

        if (!response.IsSuccess)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        var response = await _warehouseService.DeleteWarehouse(id,GetCurrentUserId());

        if (!response.IsSuccess)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetWarehouses([FromQuery] QueryParameters qp)
    {
        var response = await _warehouseService.GetWarehouses(qp);
        return Ok(response);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateWarehouse(int id, WarehouseUpdateRequest request)
    {
        var response = await _warehouseService.UpdateWarehouse(id, request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateWarehouseStatus(int id, WarehouseStatusUpdateRequest request)
    {
        var response = await _warehouseService.UpdateWarehouseStatus(id, request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

}
