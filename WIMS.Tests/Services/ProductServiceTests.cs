using System.Linq.Expressions;
using System.Threading;
using AutoMapper;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Moq;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Products;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service.Products;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace WIMS.Tests.Services.Products;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepo;
    private readonly Mock<IProductCategoryRepository> _mockCategoryRepo;
    private readonly Mock<IUnitOfMeasureRepository> _mockUomRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<ICodeGeneratorService> _mockCodeGenerator;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly InlineValidator<ProductCreateRequest> _validator;
    private readonly ProductService _service;
    private readonly ITestOutputHelper _output;

    public ProductServiceTests(ITestOutputHelper output)
    {
        _mockProductRepo = new Mock<IProductRepository>();
        _mockCategoryRepo = new Mock<IProductCategoryRepository>();
        _mockUomRepo = new Mock<IUnitOfMeasureRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockCodeGenerator = new Mock<ICodeGeneratorService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _validator = new InlineValidator<ProductCreateRequest>(); 
        _output = output;

        _service = new ProductService(
            _mockProductRepo.Object,
            _mockCategoryRepo.Object,
            _mockUomRepo.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
            _mockCodeGenerator.Object,
            _validator,
            _mockCurrentUser.Object);
    }

    private void SetupProductGetAsync(Product? product)
    {
        _mockProductRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<Product>, IQueryable<Product>>?>()))
            .ReturnsAsync(product);
    }

    private void SetupCategoryGetAsync(ProductCategory? category)
    {
        _mockCategoryRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<ProductCategory, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>?>()))
            .ReturnsAsync(category);
    }

    private void SetupUomGetAsync(UnitsOfMeasure? uom)
    {
        _mockUomRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<UnitsOfMeasure, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<UnitsOfMeasure>, IQueryable<UnitsOfMeasure>>?>()))
            .ReturnsAsync(uom);
    }

    private static Mock<IFormFile> BuildExcelFormFile(string[] headers, List<string[]>? dataRows, string fileName = "import.xlsx")
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");

        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        ws.Cell(2, 1).Value = "(sample row - ignored by import)";

        int rowIndex = 3;
        if (dataRows is not null)
        {
            foreach (var row in dataRows)
            {
                for (int c = 0; c < row.Length; c++)
                    ws.Cell(rowIndex, c + 1).Value = row[c];
                rowIndex++;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(bytes.Length);
        mockFile
            .Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken _) => target.WriteAsync(bytes, 0, bytes.Length));

        return mockFile;
    }

    private static readonly string[] TemplateHeaders = ["name", "Description", "CategoryId", "UomId", "UnitPrice", "ReorderLevel"];


    [Fact]
    public async Task CreateProduct_GivenCategoryNotFound_ReturnsNotFound()
    {
        var request = new ProductCreateRequest { Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10 };
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductCreateRequest>())).Returns((ProductCreateRequest r) => r);
        SetupCategoryGetAsync(null);

        var result = await _service.CreateProduct(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_GivenInactiveCategory_ReturnsFailure()
    {
        var request = new ProductCreateRequest { Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10 };
        var inactiveCategory = new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Inactive };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductCreateRequest>())).Returns((ProductCreateRequest r) => r);
        SetupCategoryGetAsync(inactiveCategory);

        var result = await _service.CreateProduct(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive category", result.Message);
    }

    [Fact]
    public async Task CreateProduct_GivenUomNotFound_ReturnsNotFound()
    {
        var request = new ProductCreateRequest { Name = "Widget", CategoryId = 1, UomId = 99, UnitPrice = 10 };
        var category = new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Active };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductCreateRequest>())).Returns((ProductCreateRequest r) => r);
        SetupCategoryGetAsync(category);
        SetupUomGetAsync(null);

        var result = await _service.CreateProduct(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Unit of measure", result.Message);
    }

    [Fact]
    public async Task CreateProduct_GivenDuplicateName_ReturnsFailure()
    {
        var request = new ProductCreateRequest { Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10 };
        var category = new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Active };
        var uom = new UnitsOfMeasure { Id = 1, Name = "Piece", Abbreviation = "pc" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductCreateRequest>())).Returns((ProductCreateRequest r) => r);
        SetupCategoryGetAsync(category);
        SetupUomGetAsync(uom);
        _mockProductRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>())).ReturnsAsync(true);

        var result = await _service.CreateProduct(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        _mockProductRepo.Verify(x => x.CreateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProduct_GivenValidRequest_GeneratesSkuAndReturnsSuccess()
    {
        var request = new ProductCreateRequest { Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10 };
        var category = new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Active };
        var uom = new UnitsOfMeasure { Id = 1, Name = "Piece", Abbreviation = "pc" };
        var createdProduct = new Product { Id = 7, Sku = "TEMP", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductCreateRequest>())).Returns((ProductCreateRequest r) => r);
        SetupCategoryGetAsync(category);
        SetupUomGetAsync(uom);
        _mockProductRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>())).ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Product>(It.IsAny<ProductCreateRequest>())).Returns(new Product { Sku = "TEMP", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 });
        _mockProductRepo.Setup(x => x.CreateAsync(It.IsAny<Product>())).ReturnsAsync(createdProduct);
        _mockCodeGenerator.Setup(x => x.GenerateCode("product", 7)).Returns("SKU-007");
        _mockProductRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        SetupProductGetAsync(createdProduct);
        _mockMapper.Setup(x => x.Map<ProductResponse>(It.IsAny<Product>()))
            .Returns(new ProductResponse { Id = 7, Sku = "SKU-007", Name = "Widget", Status = "Active" });

        var result = await _service.CreateProduct(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("SKU-007", createdProduct.Sku);
    }

    [Fact]
    public async Task GetProductById_GivenNotFound_ReturnsNotFound()
    {
        SetupProductGetAsync(null);

        var result = await _service.GetProductById(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetProductById_GivenExistingProduct_ReturnsSuccess()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };
        SetupProductGetAsync(product);
        _mockMapper.Setup(x => x.Map<ProductResponse>(product)).Returns(new ProductResponse { Id = 7, Sku = "SKU-007", Name = "Widget", Status = "Active" });

        var result = await _service.GetProductById(7);

        Assert.True(result.IsSuccess);
        Assert.Equal("Widget", result.Data!.Name);
    }


    [Fact]
    public async Task GetProducts_GivenValidQuery_ReturnsPagedResult()
    {
        var qp = new QueryParameters { PageNumber = 1, PageSize = 10 };
        var paged = new PagedResult<Product>
        {
            Items = [new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<QueryParameters>())).Returns((QueryParameters q) => q);
        _mockProductRepo
            .Setup(x => x.GetPaginatedAsync(It.IsAny<QueryParameters>(), It.IsAny<string[]>(), It.IsAny<Func<IQueryable<Product>, IQueryable<Product>>?>()))
            .ReturnsAsync(paged);
        _mockMapper.Setup(x => x.Map<List<ProductResponse>>(paged.Items))
            .Returns([new ProductResponse { Id = 7, Sku = "SKU-007", Name = "Widget", Status = "Active" }]);

        var result = await _service.GetProducts(qp);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Equal("Widget", result.Data.Items[0].Name);
    }

    [Fact]
    public async Task UpdateProduct_GivenNotFound_ReturnsNotFound()
    {
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductUpdateRequest>())).Returns((ProductUpdateRequest r) => r);
        SetupProductGetAsync(null);

        var result = await _service.UpdateProduct(99, new ProductUpdateRequest { Name = "New Name" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_GivenDuplicateName_ReturnsFailure()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductUpdateRequest>())).Returns((ProductUpdateRequest r) => r);
        SetupProductGetAsync(product);
        _mockProductRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>())).ReturnsAsync(true);

        var result = await _service.UpdateProduct(7, new ProductUpdateRequest { Name = "Gadget" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Widget", product.Name); 
    }

    [Fact]
    public async Task UpdateProduct_GivenNewCategoryNotFound_ReturnsNotFound()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductUpdateRequest>())).Returns((ProductUpdateRequest r) => r);
        SetupProductGetAsync(product);
        SetupCategoryGetAsync(null);

        var result = await _service.UpdateProduct(7, new ProductUpdateRequest { CategoryId = 99 });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_GivenNewCategoryInactive_ReturnsFailure()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };
        var inactiveCategory = new ProductCategory { Id = 2, Name = "Discontinued", Status = EntityStatus.Inactive };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductUpdateRequest>())).Returns((ProductUpdateRequest r) => r);
        SetupProductGetAsync(product);
        SetupCategoryGetAsync(inactiveCategory);

        var result = await _service.UpdateProduct(7, new ProductUpdateRequest { CategoryId = 2 });

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive category", result.Message);
    }

    [Fact]
    public async Task UpdateProduct_GivenNewUomNotFound_ReturnsNotFound()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductUpdateRequest>())).Returns((ProductUpdateRequest r) => r);
        SetupProductGetAsync(product);
        SetupUomGetAsync(null);

        var result = await _service.UpdateProduct(7, new ProductUpdateRequest { UomId = 99 });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_GivenPartialValidChanges_UpdatesOnlyProvidedFieldsAndReturnsSuccess()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", Description = "Old desc", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 5 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ProductUpdateRequest>())).Returns((ProductUpdateRequest r) => r);
        SetupProductGetAsync(product);
        _mockProductRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>())).ReturnsAsync(false);
        _mockProductRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<ProductResponse>(product))
            .Returns(new ProductResponse { Id = 7, Sku = "SKU-007", Name = "Premium Widget", Status = "Active" });

        var result = await _service.UpdateProduct(7, new ProductUpdateRequest { Name = "Premium Widget" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Premium Widget", product.Name);
        Assert.Equal("Old desc", product.Description);
        Assert.Equal(10, product.UnitPrice);
        Assert.Equal(5, product.ReorderLevel);
    }


    [Fact]
    public async Task UpdateProductStatus_GivenNotFound_ReturnsNotFound()
    {
        SetupProductGetAsync(null);

        var result = await _service.UpdateProductStatus(99, new ProductStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateProductStatus_GivenSameStatusRequested_ReturnsFailure()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0, Status = EntityStatus.Active };
        SetupProductGetAsync(product);

        var result = await _service.UpdateProductStatus(7, new ProductStatusUpdateRequest { Status = EntityStatus.Active });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateProductStatus_GivenInactivatingProductWithStock_ReturnsFailure()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0, Status = EntityStatus.Active };
        SetupProductGetAsync(product);
        _mockProductRepo.Setup(x => x.HasStockAsync(7)).ReturnsAsync(true);

        var result = await _service.UpdateProductStatus(7, new ProductStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Contains("has stock", result.Message);
    }

    [Fact]
    public async Task UpdateProductStatus_GivenActivatingWithCategoryAlreadyActive_UpdatesPlainlyAndReturnsSuccess()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0, Status = EntityStatus.Inactive };
        SetupProductGetAsync(product);
        _mockCategoryRepo.Setup(x => x.IsActive(1)).ReturnsAsync(true);
        _mockProductRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateProductStatus(7, new ProductStatusUpdateRequest { Status = EntityStatus.Active });

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityStatus.Active, product.Status);
        _mockProductRepo.Verify(x => x.BeginTransactionAsync(), Times.Never); // no cascade needed
    }

    [Fact]
    public async Task UpdateProductStatus_GivenActivatingWithInactiveCategory_ActivatesCategoryTooAndReturnsSuccess()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0, Status = EntityStatus.Inactive };
        var category = new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Inactive };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupProductGetAsync(product);
        _mockCategoryRepo.Setup(x => x.IsActive(1)).ReturnsAsync(false);
        SetupCategoryGetAsync(category);
        _mockCategoryRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockProductRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateProductStatus(7, new ProductStatusUpdateRequest { Status = EntityStatus.Active });

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityStatus.Active, product.Status);
        Assert.Equal(EntityStatus.Active, category.Status);
        Assert.Contains("Electronics", result.Message);
        _mockProductRepo.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProductStatus_GivenErrorDuringCategoryActivationCascade_RollsBackAndReturnsFailure()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0, Status = EntityStatus.Inactive };
        var category = new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Inactive };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupProductGetAsync(product);
        _mockCategoryRepo.Setup(x => x.IsActive(1)).ReturnsAsync(false);
        SetupCategoryGetAsync(category);
        _mockCategoryRepo.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new Exception("DB error"));

        var result = await _service.UpdateProductStatus(7, new ProductStatusUpdateRequest { Status = EntityStatus.Active });

        Assert.False(result.IsSuccess);
        _mockProductRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }


    [Fact]
    public async Task DeleteProduct_GivenNotFound_ReturnsNotFound()
    {
        SetupProductGetAsync(null);

        var result = await _service.DeleteProduct(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_GivenHasStock_ReturnsFailure()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };
        SetupProductGetAsync(product);
        _mockProductRepo.Setup(x => x.HasStockAsync(7)).ReturnsAsync(true);

        var result = await _service.DeleteProduct(7);

        Assert.False(result.IsSuccess);
        _mockProductRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<Product>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProduct_GivenNoStock_SoftDeletesAndReturnsSuccess()
    {
        var product = new Product { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 0 };
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupProductGetAsync(product);
        _mockProductRepo.Setup(x => x.HasStockAsync(7)).ReturnsAsync(false);
        _mockProductRepo.Setup(x => x.SoftDeleteAsync(product, 1)).ReturnsAsync(true);

        var result = await _service.DeleteProduct(7);

        Assert.True(result.IsSuccess);
        _mockProductRepo.Verify(x => x.SoftDeleteAsync(product, 1), Times.Once);
    }


    [Fact]
    public async Task ExportProductsToExcel_GivenProducts_ReturnsWorkbookBytesWithCorrectData()
    {
        var products = new List<Product> { new() { Id = 7, Sku = "SKU-007", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 5 } };

        _mockProductRepo
            .Setup(x => x.GetAllAsync(
                It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>?>(),
                It.IsAny<Func<IQueryable<Product>, IQueryable<Product>>?>()))
            .ReturnsAsync(products);

        _mockMapper.Setup(x => x.Map<List<ProductResponse>>(products))
            .Returns([new ProductResponse { Sku = "SKU-007", Name = "Widget", CategoryName = "Electronics", UomName = "Piece", UomAbbreviation = "pc", UnitPrice = 10, ReorderLevel = 5, Status = "Active" }]);

        var bytes = await _service.ExportProductsToExcel();

        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet("Products Report");
        Assert.Equal("SKU", ws.Cell(1, 1).GetString());
        Assert.Equal("SKU-007", ws.Cell(2, 1).GetString());
        Assert.Equal("Widget", ws.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task ExportProductsToExcel_GivenNoProducts_ReturnsWorkbookWithHeaderRowOnly()
    {
        _mockProductRepo
            .Setup(x => x.GetAllAsync(
                It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>?>(),
                It.IsAny<Func<IQueryable<Product>, IQueryable<Product>>?>()))
            .ReturnsAsync([]);

        _mockMapper.Setup(x => x.Map<List<ProductResponse>>(It.IsAny<List<Product>>())).Returns([]);

        var bytes = await _service.ExportProductsToExcel();

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet("Products Report");
        Assert.Equal("SKU", ws.Cell(1, 1).GetString());
        Assert.True(string.IsNullOrEmpty(ws.Cell(2, 1).GetString())); 
    }


    [Fact]
    public async Task ImportFile_GivenNullFile_ReturnsFailure()
    {
        var result = await _service.ImportFile(new ImportDto { File = null! });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ImportFile_GivenEmptyFile_ReturnsFailure()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        var result = await _service.ImportFile(new ImportDto { File = mockFile.Object });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ImportFile_GivenNonXlsxExtension_ReturnsFailure()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("products.csv");

        var result = await _service.ImportFile(new ImportDto { File = mockFile.Object });

        Assert.False(result.IsSuccess);
        Assert.Contains(".xlsx", result.Message);
    }

    [Fact]
    public async Task ImportFile_GivenWrongColumnHeaders_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var wrongHeaders = new[] { "Name", "Desc", "Cat", "Uom", "Price", "Reorder" };
        var file = BuildExcelFormFile(wrongHeaders, [["Widget", "desc", "1", "1", "10", "5"]]);

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Contains("Proper Formate", result.Message);
    }

    [Fact]
    public async Task ImportFile_GivenNoDataRows_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var file = BuildExcelFormFile(TemplateHeaders, dataRows: null);

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Contains("No data", result.Message);
    }

    [Theory]
    [InlineData("abc", "1", "10", "5", "CategoryId")]
    [InlineData("1", "abc", "10", "5", "uomId")]
    [InlineData("1", "1", "abc", "5", "unitPrice")]
    [InlineData("1", "1", "10", "abc", "reorderLevel")]
    public async Task ImportFile_GivenNonNumericValueInRow_RollsBackAndReturnsFailure(
        string categoryId, string uomId, string unitPrice, string reorderLevel, string expectedFieldNameInMessage)
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var row = new[] { "Widget", "desc", categoryId, uomId, unitPrice, reorderLevel };
        var file = BuildExcelFormFile(TemplateHeaders, [row]);

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedFieldNameInMessage, result.Message);
        _mockProductRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportFile_GivenCategoryNotFoundForRow_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var file = BuildExcelFormFile(TemplateHeaders, [["Widget", "desc", "99", "1", "10", "5"]]);
        SetupCategoryGetAsync(null);

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("99", result.Message);
    }

    [Fact]
    public async Task ImportFile_GivenInactiveCategoryForRow_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var file = BuildExcelFormFile(TemplateHeaders, [["Widget", "desc", "1", "1", "10", "5"]]);
        SetupCategoryGetAsync(new ProductCategory { Id = 1, Name = "Discontinued", Status = EntityStatus.Inactive });

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive category", result.Message);
    }

    [Fact]
    public async Task ImportFile_GivenUomNotFoundForRow_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var file = BuildExcelFormFile(TemplateHeaders, [["Widget", "desc", "1", "99", "10", "5"]]);
        SetupCategoryGetAsync(new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Active });
        SetupUomGetAsync(null);

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ImportFile_GivenDuplicateProductNameForRow_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var file = BuildExcelFormFile(TemplateHeaders, [["Widget", "desc", "1", "1", "10", "5"]]);
        SetupCategoryGetAsync(new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Active });
        SetupUomGetAsync(new UnitsOfMeasure { Id = 1, Name = "Piece", Abbreviation = "pc" });
        _mockProductRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>())).ReturnsAsync(true);

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Contains("Widget", result.Message);
    }

    [Fact]
    public async Task ImportFile_GivenRowFailsValidation_RollsBackAndReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var file = BuildExcelFormFile(TemplateHeaders, [["Widget", "desc", "1", "1", "10", "5"]]);
        SetupCategoryGetAsync(new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Active });
        SetupUomGetAsync(new UnitsOfMeasure { Id = 1, Name = "Piece", Abbreviation = "pc" });
        _mockProductRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>())).ReturnsAsync(false);

        _validator.RuleFor(x => x.Name).Must(_ => false).WithMessage("Forced validation failure for this test.");

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.False(result.IsSuccess);
        Assert.Contains("Forced validation failure", result.Message);
        _mockProductRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportFile_GivenAllRowsValid_CreatesProductsAndReturnsSuccess()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        var file = BuildExcelFormFile(TemplateHeaders, [["Widget", "desc", "1", "1", "10", "5"]]);
        SetupCategoryGetAsync(new ProductCategory { Id = 1, Name = "Electronics", Status = EntityStatus.Active });
        SetupUomGetAsync(new UnitsOfMeasure { Id = 1, Name = "Piece", Abbreviation = "pc" });
        _mockProductRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>())).ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Product>(It.IsAny<ProductCreateRequest>())).Returns(new Product { Sku = "TEMP", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 5 });
        _mockProductRepo.Setup(x => x.CreateAsync(It.IsAny<Product>())).ReturnsAsync(new Product { Id = 8, Sku = "TEMP", Name = "Widget", CategoryId = 1, UomId = 1, UnitPrice = 10, ReorderLevel = 5 });
        _mockCodeGenerator.Setup(x => x.GenerateCode("product", 8)).Returns("SKU-008");
        _mockProductRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.ImportFile(new ImportDto { File = file.Object });

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        _mockProductRepo.Verify(x => x.CommitTransactionAsync(), Times.Once);
        _mockProductRepo.Verify(x => x.CreateAsync(It.IsAny<Product>()), Times.Once);
    }
}