using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Products;

namespace WIMS.Application.Interfaces.Services.Products;

public interface IProductService
{
    Task<ApiResponse<ProductResponse>> CreateProduct(ProductCreateRequest request);
    Task<ApiResponse<string>> ImportFile(ImportDto request);
    Task<byte[]> ExportProductsToExcel();
    Task<ApiResponse<ProductResponse>> GetProductById(int id);
    Task<ApiResponse<PagedResult<ProductResponse>>> GetProducts(QueryParameters qp);
    Task<ApiResponse<ProductResponse>> UpdateProduct(int id, ProductUpdateRequest request);
    Task<ApiResponse<string>> UpdateProductStatus(int id, ProductStatusUpdateRequest request);
    Task<ApiResponse<string>> DeleteProduct(int id);
}
