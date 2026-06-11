using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Products;
using WIMS.Application.DTOs.UnitOfMeasure;
using WIMS.Application.Interfaces.Common;
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

    private int GetCurrentUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> CreateUnit(CreateUnitRequest request)
    {
        var response = await _unitOfMeasureService.CreateUnit(request, GetCurrentUserId());

        return response.IsSuccess ? StatusCode(201, response) : BadRequest(response);
    }
    [HttpGet]
    public async Task<IActionResult> GetUnits()
    {
        var response = await _unitOfMeasureService.GetUnitsDropdown();

        return Ok(response);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateUom(int id, UpdateUnitRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<UnitResponse>.Failure("Invalid ID", statusCode: 400));
        }
        var response = await _unitOfMeasureService.UpdateUnit(id, request);

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUom(int id)
    {
        if (id <= 0)
            return BadRequest(ApiResponse<string>.Failure("Invalid ID", statusCode: 400));

        var response = await _unitOfMeasureService.DeleteUnit(id);

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }
}
