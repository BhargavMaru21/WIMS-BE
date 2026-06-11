using WIMS.Application.DTOs;
using WIMS.Application.DTOs.ProductCategory;

namespace WIMS.Application.Interfaces.Services.ProductCategory;

public interface IProductCategoryService
{
    Task<ApiResponse<ProductCategoryResponse>> CreateCategory(ProductCategoryCreateRequest request, int createdByUserId);
    Task<ApiResponse<ProductCategoryResponse>> GetCategoryById(int id);
    Task<ApiResponse<PagedResult<ProductCategoryResponse>>> GetCategories(QueryParameters qp);
    Task<ApiResponse<List<ProductCategoryDropdownResponse>>> GetActiveCategories();
    Task<ApiResponse<ProductCategoryResponse>> UpdateCategory(int id, ProductCategoryUpdateRequest request, int modifiedByUserId);
    Task<ApiResponse<string>> UpdateCategoryStatus(int id, ProductCategoryStatusUpdateRequest request, int modifiedByUserId);
    Task<ApiResponse<string>> DeleteCategory(int id, int deletedBy);
}
