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

    private int GetCurrentUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateProduct(ProductCreateRequest request)
    {
        var response = await _productService.CreateProduct(request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return StatusCode(201, response);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "StockKeeperOrAbove")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var response = await _productService.GetProductById(id);

        if (!response.IsSuccess)
            return NotFound(response);

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Policy = "StockKeeperOrAbove")]
    public async Task<IActionResult> GetProducts([FromQuery] QueryParameters qp)
    {
        var response = await _productService.GetProducts(qp);
        return Ok(response);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateProduct(int id, ProductUpdateRequest request)
    {
        var response = await _productService.UpdateProduct(id, request, GetCurrentUserId());

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
    public async Task<IActionResult> UpdateProductStatus(int id, ProductStatusUpdateRequest request)
    {
        var response = await _productService.UpdateProductStatus(id, request, GetCurrentUserId());

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
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var response = await _productService.DeleteProduct(id, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ImportExcel([FromForm] ImportDto file)
    {
        if (file == null || file.File!.Length == 0 || !file.File.FileName.EndsWith(".xlsx"))
            return BadRequest(ApiResponse<string>.Failure("Please upload a valid .xlsx file."));

        var response = await _productService.ImportFile(file, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            if (response.StatusCode == 404)
                return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }


    [HttpGet("export")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportExcel()
    {
        var response = await _productService.GetAllProducts();

        var data = response.Data;

        //Initialize the ClosedXML Workbook and Worksheet
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products Report");

        //Populate Header Rows
        worksheet.Cell(1, 1).Value = "Sku";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Description";
        worksheet.Cell(1, 4).Value = "CategoryId";
        worksheet.Cell(1, 5).Value = "CategoryName";
        worksheet.Cell(1, 6).Value = "UomId";
        worksheet.Cell(1, 7).Value = "UomName";
        worksheet.Cell(1, 8).Value = "UomAbbreviation";
        worksheet.Cell(1, 9).Value = "UnitProce";
        worksheet.Cell(1, 10).Value = "ReorderLevel";
        worksheet.Cell(1, 11).Value = "Status";

        //Style the header row (Bold font, background color)
        var headerRange = worksheet.Range("A1:K1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        //Populate Data Rows dynamically
        int currentRow = 2;
        foreach (var product in data!)
        {
            worksheet.Cell(currentRow, 1).Value = product.Sku;
            worksheet.Cell(currentRow, 2).Value = product.Name;
            worksheet.Cell(currentRow, 3).Value = product.Description;
            worksheet.Cell(currentRow, 4).Value = product.CategoryId;
            worksheet.Cell(currentRow, 5).Value = product.CategoryName;
            worksheet.Cell(currentRow, 6).Value = product.UomId;
            worksheet.Cell(currentRow, 7).Value = product.UomName;
            worksheet.Cell(currentRow, 8).Value = product.UomAbbreviation;
            worksheet.Cell(currentRow, 9).Value = product.UnitPrice;
            worksheet.Cell(currentRow, 10).Value = product.ReorderLevel;
            worksheet.Cell(currentRow, 11).Value = product.Status;
            currentRow++;
        }

        //Automatically adjust column widths to fit content beautifully
        worksheet.Columns().AdjustToContents();

        //Write workbook data to an in-memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        
        //Reset stream position to the beginning before returning
        stream.Seek(0, SeekOrigin.Begin);

        //Return the file with proper Spreadsheet Content-Type
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        string fileName = "Products_Export.xlsx";

        return File(stream.ToArray(), contentType, fileName);
    }
}
