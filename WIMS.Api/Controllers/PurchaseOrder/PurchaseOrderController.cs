using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Application.Interfaces.Services.PurchaseOrder;

namespace WIMS.Api.Controllers.PurchaseOrder;

[ApiController]
[Route("api/manager/purchase-orders")]
[Authorize(Policy = "ManagerOrAbove")]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPoService _poService;

    public PurchaseOrderController(IPoService poService)
    {
        _poService = poService;
    }

    [HttpPost]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<ApiResponse<PoResponse>> CreatePo(PoCreateRequest request)
    {
        var response = await _poService.CreatePo(request);
        return response;
    }

    [HttpGet("{id:int}")]
    public async Task<ApiResponse<PoResponse>> GetPoById(int id)
    {
        var response = await _poService.GetPoById(id);
        return response;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<PoResponse>>> GetPos([FromQuery] QueryParameters qp)
    {
        var response = await _poService.GetPos(qp);
        return response;
    }

    [HttpPatch("{id:int}")]
    public async Task<ApiResponse<PoResponse>> UpdatePo(int id, PoUpdateRequest request)
    {
        var response = await _poService.UpdatePo(id, request);
        return response;
    }

    [HttpPost("{id:int}/items")]
    public async Task<ApiResponse<PoItemResponse>> AddItem(int id, PoItemCreateRequest request)
    {
        var response = await _poService.AddItem(id, request);
        return response;
    }

    [HttpPatch("{id:int}/items/{itemId:int}")]
    public async Task<ApiResponse<PoItemResponse>> UpdateItem(int id, int itemId, PoItemUpdateRequest request)
    {
        var response = await _poService.UpdateItem(id, itemId, request);
        return response;
    }

    [HttpDelete("{id:int}/items/{itemId:int}")]
    public async Task<ApiResponse<string>> RemoveItem(int id, int itemId)
    {
        var response = await _poService.RemoveItem(id, itemId);
        return response;
    }

    [HttpPatch("{id:int}/submit")]
    public async Task<ApiResponse<string>> SubmitPo(int id)
    {
        var response = await _poService.SubmitPo(id);
        return response;
    }

    [HttpPatch("{id:int}/approve")]
    public async Task<ApiResponse<string>> ApprovePo(int id)
    {
        var response = await _poService.ApprovePo(id);
        return response;
    }

    [HttpPatch("{id:int}/reject")]
    public async Task<ApiResponse<string>> RejectPo(int id, PoRejectRequest request)
    {
        var response = await _poService.RejectPo(id, request);
        return response;
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<ApiResponse<string>> CancelPo(int id)
    {
        var response = await _poService.CancelPo(id);
        return response;
    }
}