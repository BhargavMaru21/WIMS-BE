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

    [HttpPost]
    public async Task<ApiResponse<WarehouseResponse>> CreateWarehouse(WarehouseCreateRequest request)
    {
        var response = await _warehouseService.CreateWarehouse(request);
        return response;
    }


    [HttpGet("{id:int}")]
    public async Task<ApiResponse<WarehouseResponse>> GetWarehouseById(int id)
    {
        var response = await _warehouseService.GetWarehouseById(id);
        return response;
    }

    [HttpDelete("{id:int}")]
    public async Task<ApiResponse<string>> DeleteWarehouse(int id)
    {
        var response = await _warehouseService.DeleteWarehouse(id);
        return response;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<WarehouseResponse>>> GetWarehouses([FromQuery] QueryParameters qp)
    {
        var response = await _warehouseService.GetWarehouses(qp);
        return response;
    }

    [HttpPatch("{id:int}")]
    public async Task<ApiResponse<WarehouseResponse>> UpdateWarehouse(int id, WarehouseUpdateRequest request)
    {
        var response = await _warehouseService.UpdateWarehouse(id, request);
        return response;
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ApiResponse<string>> UpdateWarehouseStatus(int id, WarehouseStatusUpdateRequest request)
    {
        var response = await _warehouseService.UpdateWarehouseStatus(id, request);
        return response;
    }

}
