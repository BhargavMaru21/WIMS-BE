using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Products;
using WIMS.Application.Interfaces.Services.Products;
using WIMS.Domain.Entity;

namespace WIMS.Api.Controllers.Products;

[ApiController]
[Route("api/product")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ProductResponse>> CreateProduct(ProductCreateRequest request)
    {
        var response = await _productService.CreateProduct(request);
        return response;
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "StockKeeperOrAbove")]
    public async Task<ApiResponse<ProductResponse>> GetProductById(int id)
    {
        var response = await _productService.GetProductById(id);
        return response;
    }

    [HttpGet]
    [Authorize(Policy = "StockKeeperOrAbove")]
    public async Task<ApiResponse<PagedResult<ProductResponse>>> GetProducts([FromQuery] QueryParameters qp)
    {
        var response = await _productService.GetProducts(qp);
        return response;
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<ProductResponse>> UpdateProduct(int id, ProductUpdateRequest request)
    {
        var response = await _productService.UpdateProduct(id, request);
        return response;
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<string>> UpdateProductStatus(int id, ProductStatusUpdateRequest request)
    {
        var response = await _productService.UpdateProductStatus(id, request);
        return response;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<string>> DeleteProduct(int id)
    {
        var response = await _productService.DeleteProduct(id);
        return response;
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ApiResponse<string>> ImportExcel([FromForm] ImportDto file)
    {
        var response = await _productService.ImportFile(file);
        return response;
    }


    [HttpGet("export")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportExcel()
    {
        var fileBytes = await _productService.ExportProductsToExcel();
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Products_Export.xlsx");

    }
}
