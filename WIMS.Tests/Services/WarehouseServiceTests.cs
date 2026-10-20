using System.Linq.Expressions;
using AutoMapper;
using Moq;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service.WarehouseManagement;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace WIMS.Tests.Services.WarehouseManagement;

public class WarehouseServiceTests
{
    private readonly Mock<IWarehouseRepository> _mockWarehouseRepo;
    private readonly Mock<IZoneRepository> _mockZoneRepo;
    private readonly Mock<IBinRepository> _mockBinRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<ICodeGeneratorService> _mockCodeGenerator;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly WarehouseService _service;
    private readonly ITestOutputHelper _output;

    public WarehouseServiceTests(ITestOutputHelper output)
    {
        _mockWarehouseRepo = new Mock<IWarehouseRepository>();
        _mockZoneRepo = new Mock<IZoneRepository>();
        _mockBinRepo = new Mock<IBinRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockCodeGenerator = new Mock<ICodeGeneratorService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _output = output;

        _service = new WarehouseService(
            _mockWarehouseRepo.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
            _mockCodeGenerator.Object,
            _mockZoneRepo.Object,
            _mockBinRepo.Object,
            _mockCurrentUser.Object);
    }

    private void SetupWarehouseGetAsync(Warehouse? warehouse)
    {
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Warehouse, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(warehouse);
    }
    private void SetupWarehouseExistsAsync(bool value)
    {
        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(value);
    }


    //create warehouse
    [Fact]
    public async Task CreateWarehouse_GivenDuplicateName_ReturnsFailure()
    {
        var request = new WarehouseCreateRequest { Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        SetupWarehouseExistsAsync(true);

        var result = await _service.CreateWarehouse(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        _mockWarehouseRepo.Verify(x => x.CreateAsync(It.IsAny<Warehouse>()), Times.Never);
    }

    [Fact]
    public async Task CreateWarehouse_GivenValidRequest_ReturnsSuccess()
    {
        var request = new WarehouseCreateRequest { Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };
        var createdWarehouse = new Warehouse { Id = 5, Code = "TEMP", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };
        var mappedResponse = new Warehouse { Code = "TEMP", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };
        var response = new WarehouseResponse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Status = "Active" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        SetupWarehouseExistsAsync(false);
        _mockMapper.Setup(x => x.Map<Warehouse>(request)).Returns(mappedResponse);
        _mockWarehouseRepo.Setup(x => x.CreateAsync(It.IsAny<Warehouse>())).ReturnsAsync(createdWarehouse);
        _mockCodeGenerator.Setup(x => x.GenerateCode("warehouse", 5)).Returns("WH-005");
        _mockWarehouseRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<WarehouseResponse>(createdWarehouse)).Returns(response);

        var result = await _service.CreateWarehouse(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("WH-005", createdWarehouse.Code);
    }



    //get by id
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetWarehouseById_GivenInvalidId_ReturnsFailure(int invalidId)
    {
        var result = await _service.GetWarehouseById(invalidId);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetWarehouseById_WarehouseNotFound_ReturnsFailure()
    {
        SetupWarehouseGetAsync(null);

        var result = await _service.GetWarehouseById(98989);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetWarehouseById_GivenValidId_ReturnsSuccess()
    {
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };
        var mappedResponse = new WarehouseResponse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Status = "Active" };

        SetupWarehouseGetAsync(warehouse);

        _mockMapper.Setup(x => x.Map<WarehouseResponse>(warehouse)).Returns(mappedResponse);

        var result = await _service.GetWarehouseById(5);

        Assert.True(result.IsSuccess);
        Assert.Equal("MainWarehouse", result.Data!.Name);
    }


    //get warehouses 
    [Fact]
    public async Task GetWarehouses_ReturnsListOfWarehouse()
    {
        var qp = new QueryParameters { };

        var pagedResponse = new PagedResult<Warehouse>
        {
            TotalCount = 2,
            Items = new List<Warehouse>()
            {
                new Warehouse(){ Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" },
                new Warehouse(){ Id = 6, Code = "WH-006", Name = "CentralWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" }
            },
            PageNumber = 1,
            PageSize = 10
        };

        var itemResponse = new List<WarehouseResponse>()
            {
                new WarehouseResponse(){ Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" },
                new WarehouseResponse(){ Id = 6, Code = "WH-006", Name = "CentralWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" }
            };

        _mockMapper.Setup(x => x.Map<List<WarehouseResponse>>(pagedResponse.Items)).Returns(itemResponse);


        _mockInputNormalizer.Setup(x => x.NormalizeObject(qp)).Returns(qp);

        _mockWarehouseRepo.Setup(x => x.GetPaginatedAsync(
            It.IsAny<QueryParameters>(),
            It.IsAny<string[]>(),
            It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>>()
        )).ReturnsAsync(pagedResponse);

        var result = await _service.GetWarehouses(qp);

        foreach (var item in result.Data!.Items)
        {
            _output.WriteLine(item.Name);
        }

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    [Fact]
    public async Task GetWarehouses_ReturnsEmptyList()
    {
        var qp = new QueryParameters { };

        var pagedResponse = new PagedResult<Warehouse> { };

        var itemResponse = new List<WarehouseResponse> { };

        _mockMapper.Setup(x => x.Map<List<WarehouseResponse>>(pagedResponse.Items)).Returns(itemResponse);

        _mockInputNormalizer.Setup(x => x.NormalizeObject(qp)).Returns(qp);

        _mockWarehouseRepo.Setup(x => x.GetPaginatedAsync(
            It.IsAny<QueryParameters>(),
            It.IsAny<string[]>(),
            It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>>()
        )).ReturnsAsync(pagedResponse);

        var result = await _service.GetWarehouses(qp);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }



    // update warehouse 
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateWarehouse_GivenInvalidId_ReturnsFailure(int id)
    {
        var request = new WarehouseUpdateRequest { Name = "Test" };
        var result = await _service.UpdateWarehouse(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }


    [Fact]
    public async Task UpdateWarehouse_WarehouseNotFound_ReturnsFailure()
    {
        var request = new WarehouseUpdateRequest { Name = "Test" };
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        SetupWarehouseGetAsync(null);

        var result = await _service.UpdateWarehouse(98989, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateWarehouse_GivenDuplicateName_ReturnsFailure()
    {
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };
        var request = new WarehouseUpdateRequest { Name = "new name" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        SetupWarehouseGetAsync(warehouse);
        SetupWarehouseExistsAsync(true);

        var result = await _service.UpdateWarehouse(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Warehouse name already exists.", result.Message);
    }

    [Fact]
    public async Task UpdateWarehouse_GivenValidRequest_ReturnsSuccess()
    {
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };
        var request = new WarehouseUpdateRequest { Address = "New Address" };
        var mappedResponse = new WarehouseResponse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "New Address", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Status = "Active" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        SetupWarehouseGetAsync(warehouse);
        SetupWarehouseExistsAsync(false);
        _mockWarehouseRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<WarehouseResponse>(warehouse))
            .Returns(mappedResponse);

        var result = await _service.UpdateWarehouse(5, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Address", result.Data!.Address);
        Assert.Equal("MainWarehouse", result.Data!.Name);
    }


    //update warehouse status

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateWarehouseStatus_GivenInvalidId_ReturnsFailure(int id)
    {
        var request = new WarehouseStatusUpdateRequest { Status = EntityStatus.Active };
        var result = await _service.UpdateWarehouseStatus(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid Id", result.Message);
    }


    [Fact]
    public async Task UpdateWarehouseStatus_WarehouseNotFound_ReturnsFailure()
    {
        var request = new WarehouseStatusUpdateRequest { Status = EntityStatus.Inactive };
        SetupWarehouseGetAsync(null);

        var result = await _service.UpdateWarehouseStatus(99, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateWarehouseStatus_SameStatusRequested_ReturnsFailure()
    {
        var request = new WarehouseStatusUpdateRequest { Status = EntityStatus.Active };
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Status = EntityStatus.Active };
        SetupWarehouseGetAsync(warehouse);

        var result = await _service.UpdateWarehouseStatus(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateWarehouseStatus_InactivatingWhenZoneIsActive_ReturnsFailure()
    {
        var request = new WarehouseStatusUpdateRequest { Status = EntityStatus.Inactive };
        var activeZone = new Zone { Id = 1, WarehouseId = 5, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Active };
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Status = EntityStatus.Active, Zones = [activeZone] };

        SetupWarehouseGetAsync(warehouse);
        _mockWarehouseRepo.Setup(x => x.HasStockAsync(5)).ReturnsAsync(true);

        var result = await _service.UpdateWarehouseStatus(5, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("active zones and Stock", result.Message);
    }

    [Fact]
    public async Task UpdateWarehouseStatus_AllZonesInactiveWhenActivatingWarehouse_ReturnsFailure()
    {
        var request = new WarehouseStatusUpdateRequest { Status = EntityStatus.Active };
        var inactiveZone = new Zone { Id = 1, WarehouseId = 5, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Inactive };
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Status = EntityStatus.Inactive, Zones = [inactiveZone] };

        SetupWarehouseGetAsync(warehouse);
        _mockWarehouseRepo.Setup(x => x.HasStockAsync(5)).ReturnsAsync(true);

        var result = await _service.UpdateWarehouseStatus(5, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("all zones inactive", result.Message);
    }

    [Fact]
    public async Task UpdateWarehouseStatus_WithValidRequest_ReturnSuccess()
    {
        var request = new WarehouseStatusUpdateRequest { Status = EntityStatus.Active };
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Status = EntityStatus.Inactive, Zones = [] };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupWarehouseGetAsync(warehouse);
        _mockWarehouseRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateWarehouseStatus(5, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityStatus.Active, warehouse.Status);
    }


    //delete warehouse

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteWarehouse_GivenInvalidId_ReturnsFailure(int id)
    {
        var result = await _service.DeleteWarehouse(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid Id", result.Message);
    }

    [Fact]
    public async Task DeleteWarehouse_WarehouseNotFound_ReturnsFailure()
    {
        SetupWarehouseGetAsync(null);

        var result = await _service.DeleteWarehouse(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteWarehouse_WarehouseHasStock_ReturnsFailure()
    {
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };
        SetupWarehouseGetAsync(warehouse);
        _mockWarehouseRepo.Setup(x => x.HasStockAsync(5)).ReturnsAsync(true);

        var result = await _service.DeleteWarehouse(5);

        Assert.False(result.IsSuccess);
        Assert.Contains("Has Stock", result.Message);
        _mockWarehouseRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<Warehouse>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteWarehouse_WhenNoStock_SoftDeletesWarehouseWithAllZonesAndBins()
    {
        var bin = new Bin { Id = 1, ZoneId = 1, Code = "BIN-1", Name = "B1", MaxCapacity = 50 };
        var zone = new Zone { Id = 1, WarehouseId = 5, Code = "ZN-001", Name = "Cold", Bins = [bin] };
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231", Zones = [zone] };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupWarehouseGetAsync(warehouse);
        _mockWarehouseRepo.Setup(x => x.HasStockAsync(5)).ReturnsAsync(false);

        var result = await _service.DeleteWarehouse(5);

        Assert.True(result.IsSuccess);
        _mockBinRepo.Verify(x => x.SoftDeleteAsync(bin, 1), Times.Once);
        _mockZoneRepo.Verify(x => x.SoftDeleteAsync(zone, 1), Times.Once);
        _mockWarehouseRepo.Verify(x => x.SoftDeleteAsync(warehouse, 1), Times.Once);
        _mockWarehouseRepo.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteWarehouse_WhenUnexpectedErrorOccur_RollBack()
    {
        var warehouse = new Warehouse { Id = 5, Code = "WH-005", Name = "MainWarehouse", Address = "test", City = "test", ContactPerson = "test", ContactPhone = "1231231231" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupWarehouseGetAsync(warehouse);
        _mockWarehouseRepo.Setup(x => x.HasStockAsync(5)).ReturnsAsync(false);
        _mockWarehouseRepo.Setup(x => x.SoftDeleteAsync(warehouse, 1)).ThrowsAsync(new Exception("error"));

        var result = await _service.DeleteWarehouse(5);

        Assert.False(result.IsSuccess);
        _mockWarehouseRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _mockWarehouseRepo.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }
}