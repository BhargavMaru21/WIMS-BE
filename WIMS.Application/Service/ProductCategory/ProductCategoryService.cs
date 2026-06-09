using AutoMapper;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.ProductCategory;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.ProductCategory;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;

namespace WIMS.Application.Service;


public class ProductCategoryService : IProductCategoryService
{
    private readonly IProductCategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;

    public ProductCategoryService(IProductCategoryRepository categoryRepository, IMapper mapper, IInputNormalizer inputNormalizer)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
    }

    public async Task<ApiResponse<ProductCategoryResponse>> CreateCategory(ProductCategoryCreateRequest request, int createdByUserId)
    {
        request = _inputNormalizer.NormalizeObject(request);

        if (await _categoryRepository.ExistsAsync(x => x.Name.ToLower() == request.Name.ToLower()))
            return ApiResponse<ProductCategoryResponse>.Failure("A category with this name already exists.", statusCode: 400);

        var entity = _mapper.Map<ProductCategory>(request);
        entity.CreatedBy = createdByUserId;

        var newCategory = await _categoryRepository.CreateAsync(entity);
        var response = _mapper.Map<ProductCategoryResponse>(newCategory);

        return ApiResponse<ProductCategoryResponse>.Success(response, "Product category created successfully.", statusCode: 201);
    }

    public async Task<ApiResponse<ProductCategoryResponse>> GetCategoryById(int id)
    {
        var category = await _categoryRepository.GetAsync(x => x.Id == id);

        if (category is null)
            return ApiResponse<ProductCategoryResponse>.Failure("Product category not found.", statusCode: 404);

        var response = _mapper.Map<ProductCategoryResponse>(category);
        return ApiResponse<ProductCategoryResponse>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<PagedResult<ProductCategoryResponse>>> GetCategories(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);

        var paged = await _categoryRepository.GetPaginatedAsync(
            qp,
            searchableColumns: ["Name", "Description"]
        );

        var result = new PagedResult<ProductCategoryResponse>
        {
            Items = _mapper.Map<List<ProductCategoryResponse>>(paged.Items),
            TotalCount = paged.TotalCount,
            PageSize = paged.PageSize,
            PageNumber = paged.PageNumber
        };

        return ApiResponse<PagedResult<ProductCategoryResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<List<ProductCategoryDropdownResponse>>> GetActiveCategories()
    {
        var categories = await _categoryRepository.GetAllAsync(
            orderBy: q => q.OrderBy(x => x.Name),
            includes: q => q.Where(x => x.Status == EntityStatus.Active)
        );

        var response = _mapper.Map<List<ProductCategoryDropdownResponse>>(categories);
        return ApiResponse<List<ProductCategoryDropdownResponse>>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<ProductCategoryResponse>> UpdateCategory(int id, ProductCategoryUpdateRequest request, int modifiedByUserId)
    {
        request = _inputNormalizer.NormalizeObject(request);

        var category = await _categoryRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (category is null)
            return ApiResponse<ProductCategoryResponse>.Failure("Product category not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(request.Name) && await _categoryRepository.ExistsAsync(x => x.Name.ToLower() == request.Name.ToLower() && x.Id != id))
            return ApiResponse<ProductCategoryResponse>.Failure("A category with this name already exists.", statusCode: 400);

        category.Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : category.Name;
        category.Description = request.Description is not null ? request.Description : category.Description;
        category.ModifiedBy = modifiedByUserId;
        category.ModifiedAt = DateTime.UtcNow;

        await _categoryRepository.SaveChangesAsync();
        var response = _mapper.Map<ProductCategoryResponse>(category);

        return ApiResponse<ProductCategoryResponse>.Success(response, "Product category updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> UpdateCategoryStatus(int id, ProductCategoryStatusUpdateRequest request, int modifiedByUserId)
    {
        var category = await _categoryRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (category is null)
            return ApiResponse<string>.Failure("Product category not found.", statusCode: 404);

        if (category.Status == request.Status)
            return ApiResponse<string>.Failure($"Product category is already {category.Status}.", statusCode: 400);

        if (request.Status == EntityStatus.Inactive && await _categoryRepository.HasActiveProductsAsync(id))
            return ApiResponse<string>.Failure("Cannot Inactive a category that has active products. Please inactive all products in this category.", statusCode: 400);

        category.Status = request.Status;
        category.ModifiedBy = modifiedByUserId;
        category.ModifiedAt = DateTime.UtcNow;

        await _categoryRepository.SaveChangesAsync();

        return ApiResponse<string>.Success($"Product category {category.Status} successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> DeleteCategory(int id, int deletedBy)
    {
        var category = await _categoryRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (category is null)
            return ApiResponse<string>.Failure("Product category not found.", statusCode: 404);

        if (await _categoryRepository.HasActiveProductsAsync(id))
            return ApiResponse<string>.Failure("Cannot delete a category that has active products. Please inactive all products.", statusCode: 400);

        await _categoryRepository.SoftDeleteAsync(category, deletedBy);

        return ApiResponse<string>.Success("Product category deleted successfully.", statusCode: 200);
    }
}
