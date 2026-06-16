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

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ProductCategoryResponse>> CreateCategory(ProductCategoryCreateRequest request)
    {
        var response = await _categoryService.CreateCategory(request);
        return response;
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ProductCategoryResponse>> GetCategoryById(int id)
    {
        var response = await _categoryService.GetCategoryById(id);
        return response;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<PagedResult<ProductCategoryResponse>>> GetCategories([FromQuery] QueryParameters qp)
    {
        var response = await _categoryService.GetCategories(qp);
        return response;
    }

    [HttpGet("active")]
    [Authorize(Policy = "StockKeeperOrAbove")]
    public async Task<ApiResponse<List<ProductCategoryDropdownResponse>>> GetActiveCategories()
    {
        var response = await _categoryService.GetActiveCategories();
        return response;
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ProductCategoryResponse>> UpdateCategory(int id, ProductCategoryUpdateRequest request)
    {
        var response = await _categoryService.UpdateCategory(id, request);
        return response;
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<string>> UpdateCategoryStatus(int id, ProductCategoryStatusUpdateRequest request)
    {
        var response = await _categoryService.UpdateCategoryStatus(id, request);
        return response;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<string>> DeleteCategory(int id)
    {
        var response = await _categoryService.DeleteCategory(id);
        return response;
    }
}
