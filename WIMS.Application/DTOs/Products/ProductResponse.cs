namespace WIMS.Application.DTOs.Products;

public class ProductResponse
{
    public int Id { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int UomId { get; set; }
    public string UomName { get; set; } = null!;
    public string UomAbbreviation { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public decimal ReorderLevel { get; set; }
    public string Status { get; set; } = null!;
}