using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.ProductCategory;
using WIMS.Application.Interfaces.Services.ProductCategory;

namespace WIMS.Api.Controllers.ProductCategory;

[ApiController]
[Route("api/productCategory")]
[Authorize]
public class ProductCategoryController : ControllerBase
{
    private readonly IProductCategoryService _categoryService;

    public ProductCategoryController(IProductCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private int GetCurrentUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateCategory(ProductCategoryCreateRequest request)
    {
        var response = await _categoryService.CreateCategory(request, GetCurrentUserId());

        if (!response.IsSuccess)
            return BadRequest(response);

        return StatusCode(201, response);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<ProductCategoryResponse>.Failure("Invalid ID.", statusCode: 400));
        }

        var response = await _categoryService.GetCategoryById(id);

        if (!response.IsSuccess)
            return NotFound(response);

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetCategories([FromQuery] QueryParameters qp)
    {
        var response = await _categoryService.GetCategories(qp);
        return Ok(response);
    }

    [HttpGet("active")]
    [Authorize(Policy = "StockKeeperOrAbove")]
    public async Task<IActionResult> GetActiveCategories()
    {
        var response = await _categoryService.GetActiveCategories();
        return Ok(response);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateCategory(int id, ProductCategoryUpdateRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<ProductCategoryResponse>.Failure("Invalid ID.", statusCode: 400));
        }

        var response = await _categoryService.UpdateCategory(id, request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateCategoryStatus(int id, ProductCategoryStatusUpdateRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<string>.Failure("Invalid ID.", statusCode: 400));
        }

        var response = await _categoryService.UpdateCategoryStatus(id, request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<ProductCategoryResponse>.Failure("Invalid ID.", statusCode: 400));
        }

        var response = await _categoryService.DeleteCategory(id, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }
}
