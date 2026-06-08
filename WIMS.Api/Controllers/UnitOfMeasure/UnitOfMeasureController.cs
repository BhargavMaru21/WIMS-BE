using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs.UnitOfMeasure;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Services.UnitOfMeasure;

namespace WIMS.Api.Controllers.UnitOfMeasureController;

[ApiController]
[Route("api/uom")]
[Authorize(Policy = "AdminOnly")]
public class UnitOfMeasureController : ControllerBase
{
    private readonly IUnitOfMeasureService _unitOfMeasureService;
    public UnitOfMeasureController(IUnitOfMeasureService unitOfMeasureService)
    {
        _unitOfMeasureService = unitOfMeasureService;
    }

    private int GetCurrentUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> CreateUnit (CreateUnitRequest request)
    {
        var response = await _unitOfMeasureService.CreateUnit(request,GetCurrentUserId());

        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }
    [HttpGet]
    public async Task<IActionResult> GetUnits ()
    {
        var response = await _unitOfMeasureService.GetUnitsDropdown();

        return Ok(response);
    }
}
