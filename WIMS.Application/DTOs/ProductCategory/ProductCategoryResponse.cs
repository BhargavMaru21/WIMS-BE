namespace WIMS.Application.DTOs.ProductCategory;

public class ProductCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
}
