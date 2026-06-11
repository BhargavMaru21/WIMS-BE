using AutoMapper;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Products;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.Products;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;

namespace WIMS.Application.Service.Products;


public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductCategoryRepository _categoryRepository;
    private readonly IUnitOfMeasureRepository _uomRepository;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly ICodeGeneratorService _codeGeneratorService;

    public ProductService(
        IProductRepository productRepository,
        IProductCategoryRepository categoryRepository,
        IUnitOfMeasureRepository uomRepository,
        IMapper mapper,
        IInputNormalizer inputNormalizer,
        ICodeGeneratorService codeGeneratorService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _uomRepository = uomRepository;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
        _codeGeneratorService = codeGeneratorService;
    }

    public async Task<ApiResponse<ProductResponse>> CreateProduct(ProductCreateRequest request, int createdByUserId)
    {
        request = _inputNormalizer.NormalizeObject(request);

        var category = await _categoryRepository.GetAsync(x => x.Id == request.CategoryId);

        if (category is null)
            return ApiResponse<ProductResponse>.Failure("Category not found.", statusCode: 404);

        if (category.Status == EntityStatus.Inactive)
            return ApiResponse<ProductResponse>.Failure("Cannot add a product to an inactive category.", statusCode: 400);

        var uom = await _uomRepository.GetAsync(x => x.Id == request.UomId);

        if (uom is null)
            return ApiResponse<ProductResponse>.Failure("Unit of measure not found.", statusCode: 404);

        if (await _productRepository.ExistsAsync(x => x.Name.ToLower() == request.Name.ToLower()))
            return ApiResponse<ProductResponse>.Failure("A product with this name already exists.", statusCode: 400);

        await _productRepository.BeginTransactionAsync();
        try
        {
            var entity = _mapper.Map<Product>(request);
            entity.CreatedBy = createdByUserId;

            var createdProduct = await _productRepository.CreateAsync(entity);

            createdProduct.Sku = _codeGeneratorService.GenerateCode("product", createdProduct.Id);
            await _productRepository.SaveChangesAsync();

            var productWithIncludes = await _productRepository.GetAsync(x => x.Id == createdProduct.Id, includes: q => q.Include(p => p.Category).Include(p => p.Uom));

            var response = _mapper.Map<ProductResponse>(productWithIncludes);

            await _productRepository.CommitTransactionAsync();
            return ApiResponse<ProductResponse>.Success(response, "Product created successfully.", statusCode: 201);
        }
        catch (Exception)
        {
            await _productRepository.RollbackTransactionAsync();
            return ApiResponse<ProductResponse>.Failure("error occurred while creating the product.", statusCode: 500);
        }
    }

    public async Task<ApiResponse<string>> ImportFile(ImportDto request, int createdByUserId)
    {
        var templateFileColumns = new List<string> { "name", "Description", "CategoryId", "UomId", "UnitPrice", "ReorderLevel" };

        // Process file in-memory using ClosedXML
        using var stream = new MemoryStream();

        await request.File.CopyToAsync(stream);
        using (var workbook = new XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheets.First();

            //validating file formate.
            var headerRow = worksheet.FirstRow();

            if (headerRow is null)
                return ApiResponse<string>.Failure("Uploaded File is not in Proper Formate. Please Use Template File With Proper header row.");

            var actuallColumns = headerRow.CellsUsed().Select(c => c.Value.ToString().Trim()).ToList();


            var isValid = templateFileColumns.SequenceEqual(actuallColumns, StringComparer.OrdinalIgnoreCase);

            if (!isValid)
                return ApiResponse<string>.Failure("Uploaded File is not in Proper Formate. Please Use Template File.");

            var dataRowCount = worksheet.RangeUsed().RowsUsed().Skip(2).Count();

            if (dataRowCount == 0)
                return ApiResponse<string>.Failure("Uploaded File Has No data.Please Fill data with proper formate.");

            await _productRepository.BeginTransactionAsync();
            foreach (var row in worksheet.RangeUsed().RowsUsed().Skip(2))
            {
                var name = row.Cell(1).GetValue<string>().Trim();
                var description = row.Cell(2).GetValue<string>().Trim();
                var categoryId = row.Cell(3).GetValue<int>();
                var uomId = row.Cell(4).GetValue<int>();
                var unitPrice = row.Cell(5).GetValue<decimal>();
                var reorderLevel = row.Cell(6).GetValue<decimal>();

                var category = await _categoryRepository.GetAsync(x => x.Id == categoryId);

                if (category is null)
                    return ApiResponse<string>.Failure($"Category not found for categoryId : {categoryId}.No Data is Added From this file.", statusCode: 404);

                if (category.Status == EntityStatus.Inactive)
                    return ApiResponse<string>.Failure($"Cannot add a product to an inactive category. InActive CategoryId : {categoryId}.No Data is Added From this file.", statusCode: 400);

                var uom = await _uomRepository.GetAsync(x => x.Id == uomId);

                if (uom is null)
                    return ApiResponse<string>.Failure($"Unit of measure not found for UomId : {uomId}.No Data is Added From this file.", statusCode: 404);

                if (await _productRepository.ExistsAsync(x => x.Name.ToLower() == name.ToLower()))
                    return ApiResponse<string>.Failure($"A product with this name ({name}) already exists.No Data is Added From this file.", statusCode: 400);

                try
                {
                    var entity = new Product
                    {
                        Sku = "TEMP",
                        Name = name,
                        Description = description,
                        CategoryId = categoryId,
                        UomId = uomId,
                        UnitPrice = unitPrice,
                        ReorderLevel = reorderLevel,
                        CreatedBy = createdByUserId
                    };

                    var createdProduct = await _productRepository.CreateAsync(entity);

                    createdProduct.Sku = _codeGeneratorService.GenerateCode("product", createdProduct.Id);
                    await _productRepository.SaveChangesAsync();
                }
                catch (Exception)
                {
                    await _productRepository.RollbackTransactionAsync();
                    return ApiResponse<string>.Failure("error occurred while creating the products.", statusCode: 500);
                }
            }

            await _productRepository.CommitTransactionAsync();

            return ApiResponse<string>.Success("All Products created successfully.", "All Products created successfully.", statusCode: 201);
        }
    }

    public async Task<ApiResponse<ProductResponse>> GetProductById(int id)
    {
        var product = await _productRepository.GetAsync(x => x.Id == id, includes: q => q.Include(p => p.Category).Include(p => p.Uom));

        if (product is null)
            return ApiResponse<ProductResponse>.Failure("Product not found.", statusCode: 404);

        var response = _mapper.Map<ProductResponse>(product);
        return ApiResponse<ProductResponse>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<PagedResult<ProductResponse>>> GetProducts(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);

        var paged = await _productRepository.GetPaginatedAsync(
            qp,
            searchableColumns: ["Name", "Sku"],
            includes: q => q.Include(p => p.Category).Include(p => p.Uom)
        );

        var result = new PagedResult<ProductResponse>
        {
            Items = _mapper.Map<List<ProductResponse>>(paged.Items),
            TotalCount = paged.TotalCount,
            PageSize = paged.PageSize,
            PageNumber = paged.PageNumber
        };

        return ApiResponse<PagedResult<ProductResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<List<ProductResponse>>> GetAllProducts()
    {
        var allProducts = await _productRepository.GetAllAsync(orderBy: q => q.OrderBy(x => x.Sku));

        var result = _mapper.Map<List<ProductResponse>>(allProducts);

        return ApiResponse<List<ProductResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<ProductResponse>> UpdateProduct(int id, ProductUpdateRequest request, int modifiedByUserId)
    {
        request = _inputNormalizer.NormalizeObject(request);

        var product = await _productRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (product is null)
            return ApiResponse<ProductResponse>.Failure("Product not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(request.Name) && await _productRepository.ExistsAsync(x => x.Name.ToLower() == request.Name.ToLower() && x.Id != id))
            return ApiResponse<ProductResponse>.Failure("A product with this name already exists.", statusCode: 400);

        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetAsync(x => x.Id == request.CategoryId.Value);
            if (category is null)
                return ApiResponse<ProductResponse>.Failure("Category not found.", statusCode: 404);

            if (category.Status == EntityStatus.Inactive)
                return ApiResponse<ProductResponse>.Failure("Cannot assign an inactive category to a product.", statusCode: 400);
        }

        if (request.UomId.HasValue)
        {
            var uom = await _uomRepository.GetAsync(x => x.Id == request.UomId.Value);
            if (uom is null)
                return ApiResponse<ProductResponse>.Failure("Unit of measure not found.", statusCode: 404);
        }

        product.Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : product.Name;
        product.Description = request.Description is not null ? request.Description : product.Description;
        product.CategoryId = request.CategoryId ?? product.CategoryId;
        product.UomId = request.UomId ?? product.UomId;
        product.UnitPrice = request.UnitPrice ?? product.UnitPrice;
        product.ReorderLevel = request.ReorderLevel ?? product.ReorderLevel;
        product.ModifiedBy = modifiedByUserId;
        product.ModifiedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync();

        var updated = await _productRepository.GetAsync(x => x.Id == id, includes: q => q.Include(p => p.Category).Include(p => p.Uom));

        var response = _mapper.Map<ProductResponse>(updated);

        return ApiResponse<ProductResponse>.Success(response, "Product updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> UpdateProductStatus(int id, ProductStatusUpdateRequest request, int modifiedByUserId)
    {
        var product = await _productRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (product is null)
            return ApiResponse<string>.Failure("Product not found.", statusCode: 404);

        if (product.Status == request.Status)
            return ApiResponse<string>.Failure($"Product is already {product.Status}.", statusCode: 400);

        if (request.Status == EntityStatus.Inactive && await _productRepository.HasStockAsync(id))
            return ApiResponse<string>.Failure("Cannot Inactive a product that has stock. Please remove all stock.", statusCode: 400);

        if (request.Status == EntityStatus.Active && !await _categoryRepository.IsActive(product.CategoryId))
        {
            await _productRepository.BeginTransactionAsync();
            try
            {
                var category = await _categoryRepository.GetAsync(x => x.Id == product.CategoryId, useNoTracking: false);
                category!.Status = EntityStatus.Active;
                category.ModifiedBy = modifiedByUserId;
                category.ModifiedAt = DateTime.UtcNow;

                product.Status = request.Status;
                product.ModifiedBy = modifiedByUserId;
                product.ModifiedAt = DateTime.UtcNow;

                await _categoryRepository.SaveChangesAsync();
                await _productRepository.SaveChangesAsync();

                await _productRepository.CommitTransactionAsync();
                return ApiResponse<string>.Success($"Product {product.Status} successfully. The category {category.Name} was also activated.", statusCode: 200);

            }
            catch (Exception)
            {
                await _productRepository.RollbackTransactionAsync();
                return ApiResponse<string>.Failure("error occured while updating product status");
            }
        }

        product.Status = request.Status;
        product.ModifiedBy = modifiedByUserId;
        product.ModifiedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync();

        return ApiResponse<string>.Success($"Product {product.Status} successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> DeleteProduct(int id, int deletedBy)
    {
        var product = await _productRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (product is null)
            return ApiResponse<string>.Failure("Product not found.", statusCode: 404);

        if (await _productRepository.HasStockAsync(id))
            return ApiResponse<string>.Failure("Cannot delete a product that has stock. Please remove all stock.", statusCode: 400);

        await _productRepository.SoftDeleteAsync(product, deletedBy);

        return ApiResponse<string>.Success("Product deleted successfully.", statusCode: 200);
    }
}
