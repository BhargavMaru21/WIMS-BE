using System.Linq.Expressions;
using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using Moq;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.ProductCategory;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit.Abstractions;

namespace WIMS.Tests.Services;

public class ProductCategoryServiceTests
{
    private readonly Mock<IProductCategoryRepository> _mockRepo;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly ProductCategoryService _service;
    private readonly ITestOutputHelper _output;


    public ProductCategoryServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockRepo = new Mock<IProductCategoryRepository>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockMapper = new Mock<IMapper>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _service = new ProductCategoryService(
            _mockRepo.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
            _mockCurrentUser.Object);
    }


    //create
    [Fact]
    public async Task CreateCategory_GivenDuplicateCategoryName_ReturnsFailure()
    {
        var request = new ProductCategoryCreateRequest
        {
            Name = "Test",
            Description = "Optional"
        };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<ProductCategory, bool>>>())).ReturnsAsync(true);

        var result = await _service.CreateCategory(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("A category with this name already exists.", result.Message);
    }

    [Fact]
    public async Task CreateCategory_GivenValidInput_ReturnsSuccessWithCategory()
    {
        var request = new ProductCategoryCreateRequest { Name = "Test", Description = "something" };
        var mappedRequest = new ProductCategory { Name = "Test", Description = "something" };
        var entity = new ProductCategory { Id = 5, Name = "Test", Description = "something", Status = EntityStatus.Active };
        var response = new ProductCategoryResponse { Id = 5, Name = "Test", Description = "something", Status = "Active" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<ProductCategory, bool>>>())).ReturnsAsync(false);

        _mockMapper.Setup(x => x.Map<ProductCategory>(request)).Returns(mappedRequest);

        _mockRepo.Setup(x => x.CreateAsync(mappedRequest)).ReturnsAsync(entity);

        _mockMapper.Setup(x => x.Map<ProductCategoryResponse>(entity)).Returns(response);

        var result = await _service.CreateCategory(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
    }


    //getById

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCategoryById_GivenInvalidId_ReturnsFailure(int id)
    {
        var result = await _service.GetCategoryById(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid ID.", result.Message);
    }

    [Fact]
    public async Task GetCategoryById_ProductCategoryNotFound_ReturnsFailure()
    {
        _mockRepo.Setup(x => x.GetAsync(
          It.IsAny<Expression<Func<ProductCategory, bool>>>(),
          It.IsAny<bool>(),
          It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
        )).ReturnsAsync((ProductCategory?)null);

        var result = await _service.GetCategoryById(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Product category not found.", result.Message);
    }

    [Fact]
    public async Task GetCategoryById_GivenValidId_ReturnsSuccessAndCategoey()
    {
        var category = new ProductCategory { Id = 5, Name = "Test", Description = "something", Status = EntityStatus.Active };
        var response = new ProductCategoryResponse { Id = 5, Name = "Test", Description = "something", Status = "Active" };

        _mockRepo.Setup(x => x.GetAsync(
          It.IsAny<Expression<Func<ProductCategory, bool>>>(),
          It.IsAny<bool>(),
          It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
        )).ReturnsAsync(category);

        _mockMapper.Setup(x => x.Map<ProductCategoryResponse>(category)).Returns(response);

        var result = await _service.GetCategoryById(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    //getcategories

    [Fact]
    public async Task GetCategories_ReturnsPaginatedCategories()
    {
        var qp = new QueryParameters { };

        var pagedResponse = new PagedResult<ProductCategory>
        {
            TotalCount = 2,
            Items = new List<ProductCategory>()
            {
                new ProductCategory(){Id = 1 , Name = "Test" , Description = "something" , Status = EntityStatus.Active},
                new ProductCategory(){Id = 2 , Name = "Food" , Description = "something" , Status = EntityStatus.Inactive}
            },
            PageNumber = 1,
            PageSize = 10
        };

        var itemResponse = new List<ProductCategoryResponse>()
            {
                new ProductCategoryResponse(){Id = 1 , Name = "Test" , Description = "something" , Status = "Active"},
                new ProductCategoryResponse(){Id = 2 , Name = "Food" , Description = "something" , Status = "Active"}
            };

        _mockMapper.Setup(x => x.Map<List<ProductCategoryResponse>>(pagedResponse.Items)).Returns(itemResponse);


        _mockInputNormalizer.Setup(x => x.NormalizeObject(qp)).Returns(qp);

        _mockRepo.Setup(x => x.GetPaginatedAsync(
            It.IsAny<QueryParameters>(),
            It.IsAny<string[]>(),
            It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
        )).ReturnsAsync(pagedResponse);

        var result = await _service.GetCategories(qp);

        foreach (var item in result.Data!.Items)
        {
            _output.WriteLine(item.Name);
        }

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    [Fact]
    public async Task GetCategories_ReturnsEmptyList()
    {
        var qp = new QueryParameters { };

        var pagedResponse = new PagedResult<ProductCategory>
        {
            TotalCount = 0,
            Items = new List<ProductCategory> { },
            PageNumber = 1,
            PageSize = 10
        };

        var itemResponse = new List<ProductCategoryResponse> { };

        _mockMapper.Setup(x => x.Map<List<ProductCategoryResponse>>(pagedResponse.Items)).Returns(itemResponse);

        _mockInputNormalizer.Setup(x => x.NormalizeObject(qp)).Returns(qp);

        _mockRepo.Setup(x => x.GetPaginatedAsync(
            It.IsAny<QueryParameters>(),
            It.IsAny<string[]>(),
            It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
        )).ReturnsAsync(pagedResponse);

        var result = await _service.GetCategories(qp);

        _output.WriteLine("done");
        foreach (var item in result.Data!.Items)
        {
            _output.WriteLine("done");
            _output.WriteLine(item.Name);
        }

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    //GetActiveCategories

    [Fact]
    public async Task GetActiveCategories_ReturnsAllActiveCategory()
    {
        var entities = new List<ProductCategory>
        {
            new () {Id = 1 , Name = "Test" , Description = "something", Status = EntityStatus.Active},
            new () {Id = 2 , Name = "Electronics" , Description = "something", Status = EntityStatus.Active},
        };

        var response = new List<ProductCategoryDropdownResponse>
        {
            new () {Id = 1 , Name = "Test"},
            new () {Id = 2 , Name = "Electronics"},
        };

        _mockRepo.Setup(x => x.GetAllAsync(
            It.IsAny<Func<IQueryable<ProductCategory>, IOrderedQueryable<ProductCategory>>>(),
            It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
        )).ReturnsAsync(entities);

        _mockMapper.Setup(x => x.Map<List<ProductCategoryDropdownResponse>>(entities)).Returns(response);

        var result = await _service.GetActiveCategories();

        foreach (var item in result.Data!)
        {
            _output.WriteLine(item.Name);
        }

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    [Fact]
    public async Task GetActiveCategories_ReturnsEmptyList()
    {
        var entities = new List<ProductCategory> { };

        var response = new List<ProductCategoryDropdownResponse> { };

        _mockRepo.Setup(x => x.GetAllAsync(
            It.IsAny<Func<IQueryable<ProductCategory>, IOrderedQueryable<ProductCategory>>>(),
            It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
        )).ReturnsAsync(entities);

        _mockMapper.Setup(x => x.Map<List<ProductCategoryDropdownResponse>>(entities)).Returns(response);

        var result = await _service.GetActiveCategories();

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    //updateCategory

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateCategory_GivenInvalidId_ReturnsFailure(int id)
    {
        var request = new ProductCategoryUpdateRequest { };
        var result = await _service.UpdateCategory(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid ID.", result.Message);
    }


    [Fact]
    public async Task UpdateCategory_ProductCategoryNotFound_ReturnsFailure()
    {
        var request = new ProductCategoryUpdateRequest { };

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync((ProductCategory?)null);

        var result = await _service.UpdateCategory(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Product category not found.", result.Message);
    }


    [Fact]
    public async Task UpdateCategory_GivenDuplicateCategoryName_ReturnsFailure()
    {
        var request = new ProductCategoryUpdateRequest { Name = "Test" };
        var category = new ProductCategory { Id = 5, Name = "Test" };


        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync(category);

        _mockRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<ProductCategory, bool>>>())).ReturnsAsync(true);

        var result = await _service.UpdateCategory(5, request);

        Assert.NotEmpty(request.Name);
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("A category with this name already exists.", result.Message);
    }


    [Fact]
    public async Task UpdateCategory_GivenValidInput_ReturnsSuccess()
    {
        var request = new ProductCategoryUpdateRequest { Name = "UpdateTest" };
        var category = new ProductCategory { Id = 5, Name = "Test" };
        var response = new ProductCategoryResponse { Id = 5, Name = "UpdateTest" };


        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync(category);

        _mockRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<ProductCategory, bool>>>())).ReturnsAsync(false);

        _mockRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        _mockMapper.Setup(x => x.Map<ProductCategoryResponse>(category)).Returns(response);

        var result = await _service.UpdateCategory(5, request);

        _output.WriteLine(result.Data!.Name);
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("UpdateTest", result.Data.Name);
    }


    //UpdateCategoryStatus

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateCategoryStatus_GivenInvalidId_ReturnsFailure(int id)
    {
        var request = new ProductCategoryStatusUpdateRequest { };
        var result = await _service.UpdateCategoryStatus(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid ID.", result.Message);
    }

    [Fact]
    public async Task UpdateCategoryStatus_ProductCategoryNotFound_ReturnsFailure()
    {
        var request = new ProductCategoryStatusUpdateRequest { };

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync((ProductCategory?)null);

        var result = await _service.UpdateCategoryStatus(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Product category not found.", result.Message);
    }

    [Fact]
    public async Task UpdateCategoryStatus_GivenSameCategoryStatus_ReturnsFailure()
    {
        var request = new ProductCategoryStatusUpdateRequest { Status = EntityStatus.Active };
        var category = new ProductCategory { Id = 5, Name = "Test", Status = EntityStatus.Active };

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync(category);

        var result = await _service.UpdateCategoryStatus(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateCategoryStatus_CategoryHasActiveProduct_ReturnsFailure()
    {
        var request = new ProductCategoryStatusUpdateRequest { Status = EntityStatus.Inactive };
        var category = new ProductCategory { Id = 5, Name = "Test", Status = EntityStatus.Active };

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync(category);

        _mockRepo.Setup(x => x.HasActiveProductsAsync(5)).ReturnsAsync(true);

        var result = await _service.UpdateCategoryStatus(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Cannot Inactive a category that has active products. Please inactive all products in this category.", result.Message);
    }


    [Fact]
    public async Task UpdateCategoryStatus_GivenValidInput_ReturnsSuccess()
    {
        var request = new ProductCategoryStatusUpdateRequest { Status = EntityStatus.Inactive };
        var category = new ProductCategory { Id = 5, Name = "Test", Status = EntityStatus.Active };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync(category);

        _mockRepo.Setup(x => x.HasActiveProductsAsync(5)).ReturnsAsync(false);

        _mockRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateCategoryStatus(5, request);

        _output.WriteLine(result.Data);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    //DeleteCategory
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteCategory_GivenInvalidId_ReturnsFailure(int id)
    {
        var result = await _service.DeleteCategory(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid ID.", result.Message);
    }


    [Fact]
    public async Task DeleteCategory_CategoryNotFound_ReturnsFailure()
    {
        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync((ProductCategory?)null);

        var result = await _service.DeleteCategory(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Product category not found.", result.Message);
    }


    [Fact]
    public async Task DeleteCategory_CategoryHasActiveProduct_ReturnsFailure()
    {
        var category = new ProductCategory { Id = 5, Name = "Test", Status = EntityStatus.Active };

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync(category);


        _mockRepo.Setup(x => x.HasActiveProductsAsync(category.Id)).ReturnsAsync(true);

        var result = await _service.DeleteCategory(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Cannot delete a category that has active products. Please inactive all products.", result.Message);
    }

    [Fact]
    public async Task DeleteCategory_GivenValidId_ReturnsSuccess()
    {
        var category = new ProductCategory { Id = 5, Name = "Test", Status = EntityStatus.Active };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        _mockRepo.Setup(x => x.GetAsync(
        It.IsAny<Expression<Func<ProductCategory, bool>>>(),
        It.IsAny<bool>(),
        It.IsAny<Func<IQueryable<ProductCategory>, IQueryable<ProductCategory>>>()
      )).ReturnsAsync(category);

        _mockRepo.Setup(x => x.HasActiveProductsAsync(category.Id)).ReturnsAsync(false);

        _mockRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.DeleteCategory(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Product category deleted successfully.", result.Data);
    }

}
