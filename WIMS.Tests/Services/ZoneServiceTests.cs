using System.Linq.Expressions;
using AutoMapper;
using Moq;
using WIMS.Application.DTOs.Warehouse;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service.WarehouseManagement;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace WIMS.Tests.Services.WarehouseManagement;

public class ZoneServiceTests
{
    private readonly Mock<IZoneRepository> _mockZoneRepo;
    private readonly Mock<IWarehouseRepository> _mockWarehouseRepo;
    private readonly Mock<IBinRepository> _mockBinRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<ICodeGeneratorService> _mockCodeGenerator;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly ZoneService _service;
    private readonly ITestOutputHelper _output;

    public ZoneServiceTests(ITestOutputHelper output)
    {
        _mockZoneRepo = new Mock<IZoneRepository>();
        _mockWarehouseRepo = new Mock<IWarehouseRepository>();
        _mockBinRepo = new Mock<IBinRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockCodeGenerator = new Mock<ICodeGeneratorService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _output = output;

        _service = new ZoneService(
            _mockZoneRepo.Object,
            _mockWarehouseRepo.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
            _mockCodeGenerator.Object,
            _mockBinRepo.Object,
            _mockCurrentUser.Object);
    }

    private void SetupZoneGetAsync(Zone? zone)
    {
        _mockZoneRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Zone, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<Zone>, IQueryable<Zone>>?>()))
            .ReturnsAsync(zone);
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

    private void SetupWZoneExistsAsync(bool value)
    {
        _mockZoneRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Zone, bool>>>())).ReturnsAsync(value);
    }


    [Fact]
    public async Task CreateZone_GivenWarehouseNotFound_ReturnsNotFound()
    {
        var request = new ZoneCreateRequest { WarehouseId = 1, Name = "Cold Storage" };
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ZoneCreateRequest>())).Returns((ZoneCreateRequest r) => r);
        SetupWarehouseGetAsync(null);

        var result = await _service.CreateZone(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateZone_GivenInactiveWarehouse_ReturnsFailure()
    {
        var request = new ZoneCreateRequest { WarehouseId = 1, Name = "Cold Storage" };
        var inactiveWarehouse = new Warehouse
        {
            Code = "WH-1",
            Name = "Main",
            Address = "A",
            City = "C",
            ContactPerson = "P",
            ContactPhone = "9999999999",
            Status = EntityStatus.Inactive
        };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ZoneCreateRequest>())).Returns((ZoneCreateRequest r) => r);
        SetupWarehouseGetAsync(inactiveWarehouse);

        var result = await _service.CreateZone(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("inactive warehouse", result.Message);
    }

    [Fact]
    public async Task CreateZone_GivenDuplicateNameInWarehouse_ReturnsFailure()
    {
        var request = new ZoneCreateRequest { WarehouseId = 1, Name = "Cold Storage" };
        var activeWarehouse = new Warehouse
        {
            Code = "WH-1",
            Name = "Main",
            Address = "A",
            City = "C",
            ContactPerson = "P",
            ContactPhone = "9999999999",
            Status = EntityStatus.Active
        };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ZoneCreateRequest>())).Returns((ZoneCreateRequest r) => r);
        SetupWarehouseGetAsync(activeWarehouse);
        SetupWZoneExistsAsync(true);

        var result = await _service.CreateZone(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateZone_GivenValidRequest_ReturnsSuccess()
    {
        var request = new ZoneCreateRequest { WarehouseId = 1, Name = "Cold Storage" };
        var activeWarehouse = new Warehouse
        {
            Id = 1,
            Code = "WH-1",
            Name = "Main",
            Address = "A",
            City = "C",
            ContactPerson = "P",
            ContactPhone = "9999999999",
            Status = EntityStatus.Active
        };
        var createdZone = new Zone { Id = 5, WarehouseId = 1, Code = "TEMP", Name = "Cold Storage" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ZoneCreateRequest>())).Returns((ZoneCreateRequest r) => r);
        SetupWarehouseGetAsync(activeWarehouse);

        SetupWZoneExistsAsync(false);
        _mockMapper.Setup(x => x.Map<Zone>(It.IsAny<ZoneCreateRequest>())).Returns(new Zone { WarehouseId = 1, Code = "TEMP", Name = "Cold Storage" });
        _mockZoneRepo.Setup(x => x.CreateAsync(It.IsAny<Zone>())).ReturnsAsync(createdZone);
        _mockCodeGenerator.Setup(x => x.GenerateCode("zone", 5)).Returns("ZN-005");

        SetupZoneGetAsync(createdZone);

        _mockMapper.Setup(x => x.Map<ZoneResponse>(It.IsAny<Zone>()))
            .Returns(new ZoneResponse { Id = 5, Code = "ZN-005", Name = "Cold Storage", WarehouseId = 1, WarehouseName = "Main", Status = "Active" });

        var result = await _service.CreateZone(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("ZN-005", result.Data!.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task GetZoneById_GivenInvalidId_ReturnsBadRequest(int invalidId)
    {
        var result = await _service.GetZoneById(invalidId);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetZoneById_GivenNotFound_ReturnsNotFound()
    {
        SetupZoneGetAsync(null);

        var result = await _service.GetZoneById(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }


    [Fact]
    public async Task UpdateZone_GivenDuplicateNameInSameWarehouse_ReturnsFailure()
    {
        var existingZone = new Zone { Id = 5, WarehouseId = 1, Code = "ZN-005", Name = "Cold Storage" };
        var request = new ZoneUpdateRequest { Name = "Dry Storage" };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ZoneUpdateRequest>())).Returns((ZoneUpdateRequest r) => r);
        SetupZoneGetAsync(existingZone);
        SetupWZoneExistsAsync(true);

        var result = await _service.UpdateZone(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Cold Storage", existingZone.Name);
    }

    [Fact]
    public async Task UpdateZone_GivenValidRequest_UpdatesNameAndReturnsSuccess()
    {
        var existingZone = new Zone { Id = 5, WarehouseId = 1, Code = "ZN-005", Name = "Cold Storage" };
        var request = new ZoneUpdateRequest { Name = "Dry Storage" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ZoneUpdateRequest>())).Returns((ZoneUpdateRequest r) => r);
        SetupZoneGetAsync(existingZone);
        SetupWZoneExistsAsync(false);
        _mockMapper.Setup(x => x.Map<ZoneResponse>(It.IsAny<Zone>()))
            .Returns(new ZoneResponse { Id = 5, Name = "Dry Storage", Code = "ZN-005", WarehouseId = 1, WarehouseName = "Main", Status = "Active" });

        var result = await _service.UpdateZone(5, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dry Storage", existingZone.Name);
        _mockZoneRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateZoneStatus_GivenSameStatusRequested_ReturnsFailure()
    {
        var zone = new Zone { Id = 5, WarehouseId = 1, Code = "ZN-005", Name = "Cold Storage", Status = EntityStatus.Active };
        SetupZoneGetAsync(zone);

        var result = await _service.UpdateZoneStatus(5, new ZoneStatusUpdateRequest { Status = EntityStatus.Active });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateZoneStatus_GivenZoneIsNotLastActiveInWarehouse_InactivatesWithoutTouchingWarehouse()
    {
        var zone = new Zone { Id = 5, WarehouseId = 1, Code = "ZN-005", Name = "Cold Storage", Status = EntityStatus.Active };
        SetupZoneGetAsync(zone);

        _mockZoneRepo.Setup(x => x.GetActiveZoneByWarehouseAsync(1)).ReturnsAsync([zone, new Zone { Id = 6, WarehouseId = 1, Code = "ZN-006", Name = "Dry" }]);

        var result = await _service.UpdateZoneStatus(5, new ZoneStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityStatus.Inactive, zone.Status);
        _mockWarehouseRepo.Verify(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()), Times.Never);
    }


    [Fact]
    public async Task DeleteZone_GivenNotFound_ReturnsNotFound()
    {
        SetupZoneGetAsync(null);

        var result = await _service.DeleteZone(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteZone_GivenAnyBinHasStock_ReturnsFailureAndDoesNotDeleteAnything()
    {
        var bin = new Bin { Id = 10, ZoneId = 5, Code = "BIN-010", Name = "B1", MaxCapacity = 100 };
        var zone = new Zone { Id = 5, WarehouseId = 1, Code = "ZN-005", Name = "Cold Storage", Bins = [bin] };
        SetupZoneGetAsync(zone);
        _mockBinRepo.Setup(x => x.HasStockAsync(10)).ReturnsAsync(true);

        var result = await _service.DeleteZone(5);

        Assert.False(result.IsSuccess);
        _mockZoneRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<Zone>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteZone_GivenNoBinsHaveStock_SoftDeletesZoneAndAllBins()
    {
        var bin1 = new Bin { Id = 10, ZoneId = 5, Code = "BIN-010", Name = "B1", MaxCapacity = 100 };
        var bin2 = new Bin { Id = 11, ZoneId = 5, Code = "BIN-011", Name = "B2", MaxCapacity = 100 };
        var zone = new Zone { Id = 5, WarehouseId = 1, Code = "ZN-005", Name = "Cold Storage", Bins = [bin1, bin2] };

        SetupZoneGetAsync(zone);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockBinRepo.Setup(x => x.HasStockAsync(It.IsAny<int>())).ReturnsAsync(false);

        var result = await _service.DeleteZone(5);

        Assert.True(result.IsSuccess);
        _mockBinRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<Bin>(), 1), Times.Exactly(2));
        _mockZoneRepo.Verify(x => x.SoftDeleteAsync(zone, 1), Times.Once);
    }
}