using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Application.Interfaces.Services.PurchaseOrder;

namespace WIMS.Api.Controllers.PurchaseOrder;

[ApiController]
[Route("api/purchaseOrder")]
[Authorize]

public class PurchaseOrderController(IPurchaseOrderService purchaseOrderService) : ControllerBase
{
    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int GetWarehouseId() => int.Parse(User.FindFirstValue("WarehouseId")!);

    [HttpPost]
    [Authorize(Roles = "WarehouseManager")]
    public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderCreateRequest request)
    {
        var response =  await purchaseOrderService.CreatePurchaseOrder(request,GetCurrentUserId(), GetWarehouseId());

        if(!response.IsSuccess){
            StatusCode(500,response);
        }
        
        return Ok(response);
    }
}
