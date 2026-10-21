using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.GoodsReceipts;
using WIMS.Application.Interfaces.Services;

namespace WIMS.Api.Controllers.GoodsReceipt;

[ApiController]
[Route("api/goods-receipts")]
[Authorize(Policy = "StockKeeperOrAbove")]
public class GoodsReceiptController : ControllerBase
{
    private readonly IGrService _GrService;

    public GoodsReceiptController(IGrService GrService)
    {
        _GrService = GrService;
    }

    [HttpPost]
    [Authorize(Roles = "StockKeeper")]
    public async Task<ApiResponse<GrResponse>> CreateGr(GrCreateRequest request)
    {
        var response = await _GrService.CreateGr(request);
        return response;
    }

    [HttpGet("pending-pos")]
    [Authorize(Roles = "StockKeeper")]
    public async Task<ApiResponse<List<PendingPoResponse>>> GetPendingPos()
    {
        var response = await _GrService.GetPendingPos();
        return response;
    }

    [HttpGet("{id:int}")]
    public async Task<ApiResponse<GrResponse>> GetGrById(int id)
    {
        var response = await _GrService.GetGrById(id);
        return response;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResult<GrResponse>>> GetGrs([FromQuery] QueryParameters qp)
    {
        var response = await _GrService.GetGrs(qp);
        return response;
    }
}
