namespace WIMS.Application.DTOs.Products;

public class ProductUpdateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int? UomId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? ReorderLevel { get; set; }
}