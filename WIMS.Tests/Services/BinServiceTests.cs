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

namespace WIMS.Tests.Services.WarehouseManagement;

public class BinServiceTests
{
    private readonly Mock<IBinRepository> _mockBinRepo;
    private readonly Mock<IZoneRepository> _mockZoneRepo;
    private readonly Mock<IWarehouseRepository> _mockWarehouseRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<ICodeGeneratorService> _mockCodeGenerator;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly ZoneService _realZoneService;
    private readonly BinService _service;

    public BinServiceTests()
    {
        _mockBinRepo = new Mock<IBinRepository>();
        _mockZoneRepo = new Mock<IZoneRepository>();
        _mockWarehouseRepo = new Mock<IWarehouseRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockCodeGenerator = new Mock<ICodeGeneratorService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _realZoneService = new ZoneService(
            _mockZoneRepo.Object,
            _mockWarehouseRepo.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
            _mockCodeGenerator.Object,
            _mockBinRepo.Object,
            _mockCurrentUser.Object);

        _service = new BinService(
            _mockBinRepo.Object,
            _mockZoneRepo.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
            _mockCodeGenerator.Object,
            _realZoneService,
            _mockCurrentUser.Object);
    }

    private void SetupBinGetAsync(Bin? bin)
    {
        _mockBinRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Bin, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<Bin>, IQueryable<Bin>>?>()))
            .ReturnsAsync(bin);
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


    [Fact]
    public async Task CreateBin_GivenZoneNotFound_ReturnsNotFound()
    {
        var request = new BinCreateRequest { ZoneId = 1, Name = "B1" };
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<BinCreateRequest>())).Returns((BinCreateRequest r) => r);
        SetupZoneGetAsync(null);

        var result = await _service.CreateBin(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateBin_GivenInactiveZone_ReturnsFailure()
    {
        var request = new BinCreateRequest { ZoneId = 1, Name = "B1" };
        var inactiveZone = new Zone { Id = 1, WarehouseId = 1, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Inactive };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<BinCreateRequest>())).Returns((BinCreateRequest r) => r);
        SetupZoneGetAsync(inactiveZone);

        var result = await _service.CreateBin(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive zone", result.Message);
    }

    [Fact]
    public async Task CreateBin_GivenDuplicateNameInZone_ReturnsFailure()
    {
        var request = new BinCreateRequest { ZoneId = 1, Name = "B1" };
        var zone = new Zone { Id = 1, WarehouseId = 1, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Active };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<BinCreateRequest>())).Returns((BinCreateRequest r) => r);
        SetupZoneGetAsync(zone);
        _mockBinRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bin, bool>>>())).ReturnsAsync(true);

        var result = await _service.CreateBin(request);

        Assert.False(result.IsSuccess);
        _mockBinRepo.Verify(x => x.CreateAsync(It.IsAny<Bin>()), Times.Never);
    }

    [Fact]
    public async Task CreateBin_GivenValidRequest_GeneratesCodeAndReturnsSuccess()
    {
        var request = new BinCreateRequest { ZoneId = 1, Name = "B1" };
        var zone = new Zone { Id = 1, WarehouseId = 1, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Active };
        var createdBin = new Bin { Id = 20, ZoneId = 1, Code = "TEMP", Name = "B1", MaxCapacity = 0 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<BinCreateRequest>())).Returns((BinCreateRequest r) => r);
        SetupZoneGetAsync(zone);
        _mockBinRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bin, bool>>>())).ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Bin>(It.IsAny<BinCreateRequest>())).Returns(new Bin { ZoneId = 1, Code = "TEMP", Name = "B1", MaxCapacity = 0 });
        _mockBinRepo.Setup(x => x.CreateAsync(It.IsAny<Bin>())).ReturnsAsync(createdBin);
        _mockCodeGenerator.Setup(x => x.GenerateCode("bin", 20)).Returns("BIN-020");
        _mockBinRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        SetupBinGetAsync(createdBin);
        _mockMapper.Setup(x => x.Map<BinResponse>(It.IsAny<Bin>()))
            .Returns(new BinResponse { Id = 20, Code = "BIN-020", Name = "B1", ZoneId = 1, ZoneName = "Cold", WarehouseId = 1, WarehouseName = "Main", Status = "Active" });

        var result = await _service.CreateBin(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("BIN-020", createdBin.Code);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetBinById_GivenInvalidId_ReturnsBadRequest(int invalidId)
    {
        var result = await _service.GetBinById(invalidId);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetBinById_GivenNotFound_ReturnsNotFound()
    {
        SetupBinGetAsync(null);

        var result = await _service.GetBinById(20);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetBinById_GivenExistingBin_ReturnsSuccess()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0 };
        SetupBinGetAsync(bin);
        _mockMapper.Setup(x => x.Map<BinResponse>(bin))
            .Returns(new BinResponse { Id = 20, Code = "BIN-020", Name = "B1", Status = "Active" });

        var result = await _service.GetBinById(20);

        Assert.True(result.IsSuccess);
        Assert.Equal("B1", result.Data!.Name);
    }


    [Fact]
    public async Task GetBins_GivenValidQuery_ReturnsPagedResult()
    {
        var qp = new QueryParameters { PageNumber = 1, PageSize = 10 };
        var paged = new PagedResult<Bin>
        {
            Items = [new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0 }],
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<QueryParameters>())).Returns((QueryParameters q) => q);
        _mockBinRepo
            .Setup(x => x.GetPaginatedAsync(It.IsAny<QueryParameters>(), It.IsAny<string[]>(), It.IsAny<Func<IQueryable<Bin>, IQueryable<Bin>>?>()))
            .ReturnsAsync(paged);
        _mockMapper.Setup(x => x.Map<List<BinResponse>>(paged.Items))
            .Returns([new BinResponse { Id = 20, Code = "BIN-020", Name = "B1", Status = "Active" }]);

        var result = await _service.GetBins(qp);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }


    [Fact]
    public async Task GetBinsDropdown_GivenNoFilters_ReturnsAllBins()
    {
        var bins = new List<Bin>
        {
            new() { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Zone = new Zone { Id = 1, WarehouseId = 1, Code = "ZN-001", Name = "Cold" } },
            new() { Id = 21, ZoneId = 2, Code = "BIN-021", Name = "B2", MaxCapacity = 0, Zone = new Zone { Id = 2, WarehouseId = 2, Code = "ZN-002", Name = "Dry" } }
        };

        _mockBinRepo
            .Setup(x => x.GetAllAsync(
                It.IsAny<Func<IQueryable<Bin>, IOrderedQueryable<Bin>>?>(),
                It.IsAny<Func<IQueryable<Bin>, IQueryable<Bin>>?>()))
            .ReturnsAsync(bins);
        _mockMapper.Setup(x => x.Map<List<BinDropdownResponse>>(It.Is<List<Bin>>(l => l.Count == 2)))
            .Returns([new BinDropdownResponse { Id = 20 }, new BinDropdownResponse { Id = 21 }]);

        var result = await _service.GetBinsDropdown();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetBinsDropdown_GivenZoneIdFilter_ReturnsOnlyMatchingZoneBins()
    {
        var bins = new List<Bin>
        {
            new() { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Zone = new Zone { Id = 1, WarehouseId = 1, Code = "ZN-001", Name = "Cold" } },
            new() { Id = 21, ZoneId = 2, Code = "BIN-021", Name = "B2", MaxCapacity = 0, Zone = new Zone { Id = 2, WarehouseId = 2, Code = "ZN-002", Name = "Dry" } }
        };

        _mockBinRepo
            .Setup(x => x.GetAllAsync(
                It.IsAny<Func<IQueryable<Bin>, IOrderedQueryable<Bin>>?>(),
                It.IsAny<Func<IQueryable<Bin>, IQueryable<Bin>>?>()))
            .ReturnsAsync(bins);
        _mockMapper.Setup(x => x.Map<List<BinDropdownResponse>>(It.Is<List<Bin>>(l => l.Count == 1 && l[0].Id == 20)))
            .Returns([new BinDropdownResponse { Id = 20 }]);

        var result = await _service.GetBinsDropdown(zoneId: 1);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetBinsDropdown_GivenWarehouseIdFilter_ReturnsOnlyMatchingWarehouseBins()
    {
        var bins = new List<Bin>
        {
            new() { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Zone = new Zone { Id = 1, WarehouseId = 1, Code = "ZN-001", Name = "Cold" } },
            new() { Id = 21, ZoneId = 2, Code = "BIN-021", Name = "B2", MaxCapacity = 0, Zone = new Zone { Id = 2, WarehouseId = 2, Code = "ZN-002", Name = "Dry" } }
        };

        _mockBinRepo
            .Setup(x => x.GetAllAsync(
                It.IsAny<Func<IQueryable<Bin>, IOrderedQueryable<Bin>>?>(),
                It.IsAny<Func<IQueryable<Bin>, IQueryable<Bin>>?>()))
            .ReturnsAsync(bins);
        _mockMapper.Setup(x => x.Map<List<BinDropdownResponse>>(It.Is<List<Bin>>(l => l.Count == 1 && l[0].Id == 21)))
            .Returns([new BinDropdownResponse { Id = 21 }]);

        var result = await _service.GetBinsDropdown(warehouseId: 2);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateBin_GivenInvalidId_ReturnsBadRequest(int invalidId)
    {
        var result = await _service.UpdateBin(invalidId, new BinUpdateRequest { Name = "X" });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateBin_GivenNotFound_ReturnsNotFound()
    {
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<BinUpdateRequest>())).Returns((BinUpdateRequest r) => r);
        SetupBinGetAsync(null);

        var result = await _service.UpdateBin(20, new BinUpdateRequest { Name = "New" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateBin_GivenDuplicateNameInZone_ReturnsFailure()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0 };
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<BinUpdateRequest>())).Returns((BinUpdateRequest r) => r);
        SetupBinGetAsync(bin);
        _mockBinRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bin, bool>>>())).ReturnsAsync(true);

        var result = await _service.UpdateBin(20, new BinUpdateRequest { Name = "B2" });

        Assert.False(result.IsSuccess);
        Assert.Equal("B1", bin.Name);
    }

    [Fact]
    public async Task UpdateBin_GivenPartialValidChanges_UpdatesOnlyProvidedFieldsAndReturnsSuccess()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 50 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<BinUpdateRequest>())).Returns((BinUpdateRequest r) => r);
        SetupBinGetAsync(bin);
        _mockBinRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bin, bool>>>())).ReturnsAsync(false);
        _mockBinRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<BinResponse>(bin))
            .Returns(new BinResponse { Id = 20, Name = "B1", MaxCapacity = 100, Status = "Active" });

        var result = await _service.UpdateBin(20, new BinUpdateRequest { MaxCapacity = 100 });

        Assert.True(result.IsSuccess);
        Assert.Equal(100, bin.MaxCapacity);
        Assert.Equal("B1", bin.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteBin_GivenInvalidId_ReturnsBadRequest(int invalidId)
    {
        var result = await _service.DeleteBin(invalidId);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task DeleteBin_GivenNotFound_ReturnsNotFound()
    {
        SetupBinGetAsync(null);

        var result = await _service.DeleteBin(20);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteBin_GivenHasStock_ReturnsFailure()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0 };
        SetupBinGetAsync(bin);
        _mockBinRepo.Setup(x => x.HasStockAsync(20)).ReturnsAsync(true);

        var result = await _service.DeleteBin(20);

        Assert.False(result.IsSuccess);
        _mockBinRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<Bin>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBin_GivenNoStock_SoftDeletesAndReturnsSuccess()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0 };
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupBinGetAsync(bin);
        _mockBinRepo.Setup(x => x.HasStockAsync(20)).ReturnsAsync(false);
        _mockBinRepo.Setup(x => x.SoftDeleteAsync(bin, 1)).ReturnsAsync(true);

        var result = await _service.DeleteBin(20);

        Assert.True(result.IsSuccess);
        _mockBinRepo.Verify(x => x.SoftDeleteAsync(bin, 1), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateBinStatus_GivenInvalidId_ReturnsBadRequest(int invalidId)
    {
        var result = await _service.UpdateBinStatus(invalidId, new BinStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateBinStatus_GivenNotFound_ReturnsNotFound()
    {
        SetupBinGetAsync(null);

        var result = await _service.UpdateBinStatus(20, new BinStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateBinStatus_GivenSameStatusRequested_ReturnsFailure()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Status = EntityStatus.Active };
        SetupBinGetAsync(bin);

        var result = await _service.UpdateBinStatus(20, new BinStatusUpdateRequest { Status = EntityStatus.Active });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateBinStatus_GivenInactivatingBinWithStock_ReturnsFailure()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Status = EntityStatus.Active };
        SetupBinGetAsync(bin);
        _mockBinRepo.Setup(x => x.HasStockAsync(20)).ReturnsAsync(true);

        var result = await _service.UpdateBinStatus(20, new BinStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Contains("holds stock", result.Message);
    }

    [Fact]
    public async Task UpdateBinStatus_GivenNotFirstOrLastBinInZone_PlainStatusChangeSucceeds()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Status = EntityStatus.Active };
        SetupBinGetAsync(bin);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockBinRepo.Setup(x => x.HasStockAsync(20)).ReturnsAsync(false);

        _mockBinRepo.Setup(x => x.GetActiveBinByZoneAsync(1))
            .ReturnsAsync([bin, new Bin { Id = 21, ZoneId = 1, Code = "BIN-021", Name = "B2", MaxCapacity = 0 }]);

        var result = await _service.UpdateBinStatus(20, new BinStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityStatus.Inactive, bin.Status);
        _mockZoneRepo.Verify(x => x.GetAsync(It.IsAny<Expression<Func<Zone, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Zone>, IQueryable<Zone>>?>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBinStatus_GivenInactivatingLastActiveBinInZone_CascadesToInactivateZoneAndWarehouse()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Status = EntityStatus.Active };
        var zone = new Zone { Id = 1, WarehouseId = 100, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Active };
        var warehouse = new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Active };

        SetupBinGetAsync(bin);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockBinRepo.Setup(x => x.HasStockAsync(20)).ReturnsAsync(false);

        _mockBinRepo.Setup(x => x.GetActiveBinByZoneAsync(1)).ReturnsAsync([bin]);
        _mockZoneRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        SetupZoneGetAsync(zone);
        SetupWarehouseGetAsync(warehouse); 
        _mockWarehouseRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateBinStatus(20, new BinStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityStatus.Inactive, bin.Status);
        Assert.Equal(EntityStatus.Inactive, zone.Status);
        Assert.Equal(EntityStatus.Inactive, warehouse.Status); 
        _mockBinRepo.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateBinStatus_GivenActivatingFirstBinInZone_CascadesToActivateZoneAndWarehouse()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Status = EntityStatus.Inactive };
        var zone = new Zone { Id = 1, WarehouseId = 100, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Inactive };
        var warehouse = new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Inactive };

        SetupBinGetAsync(bin);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        _mockBinRepo.Setup(x => x.GetActiveBinByZoneAsync(1)).ReturnsAsync([]);
        _mockZoneRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        SetupZoneGetAsync(zone);
        SetupWarehouseGetAsync(warehouse);
        _mockWarehouseRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateBinStatus(20, new BinStatusUpdateRequest { Status = EntityStatus.Active });

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityStatus.Active, bin.Status);
        Assert.Equal(EntityStatus.Active, zone.Status);
        Assert.Equal(EntityStatus.Active, warehouse.Status);
    }

    [Fact]
    public async Task UpdateBinStatus_GivenWarehouseNotFoundDuringCascade_RollsBackAndReturnsFailure()
    {
        var bin = new Bin { Id = 20, ZoneId = 1, Code = "BIN-020", Name = "B1", MaxCapacity = 0, Status = EntityStatus.Active };
        var zone = new Zone { Id = 1, WarehouseId = 100, Code = "ZN-001", Name = "Cold", Status = EntityStatus.Active };

        SetupBinGetAsync(bin);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockBinRepo.Setup(x => x.HasStockAsync(20)).ReturnsAsync(false);
        _mockBinRepo.Setup(x => x.GetActiveBinByZoneAsync(1)).ReturnsAsync([bin]);
        _mockZoneRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        SetupZoneGetAsync(zone);

        SetupWarehouseGetAsync(null);

        var result = await _service.UpdateBinStatus(20, new BinStatusUpdateRequest { Status = EntityStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.StatusCode);
        _mockBinRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _mockBinRepo.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }
}