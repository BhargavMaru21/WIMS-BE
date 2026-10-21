using System.Linq.Expressions;
using AutoMapper;
using FluentValidation;
using Moq;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit;

// namespace WIMS.Tests.Services.PurchaseOrder;
public class PoServiceTests
{
    private readonly Mock<IPoRepository> _mockPoRepo;
    private readonly Mock<IPoItemRepository> _mockPoItemRepo;
    private readonly Mock<IWarehouseRepository> _mockWarehouseRepo;
    private readonly Mock<IProductRepository> _mockProductRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<ICodeGeneratorService> _mockCodeGenerator;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly InlineValidator<PoItemCreateRequest> _validator;
    private readonly PoService _service;

    public PoServiceTests()
    {
        _mockPoRepo = new Mock<IPoRepository>();
        _mockPoItemRepo = new Mock<IPoItemRepository>();
        _mockWarehouseRepo = new Mock<IWarehouseRepository>();
        _mockProductRepo = new Mock<IProductRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockCodeGenerator = new Mock<ICodeGeneratorService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _validator = new InlineValidator<PoItemCreateRequest>();

        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<PoCreateRequest>())).Returns((PoCreateRequest r) => r);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<PoUpdateRequest>())).Returns((PoUpdateRequest r) => r);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<QueryParameters>())).Returns((QueryParameters q) => q);
        _mockInputNormalizer.Setup(x => x.Normalize(It.IsAny<string>())).Returns((string s) => s);

        _service = new PoService(
            _mockPoRepo.Object,
            _mockPoItemRepo.Object,
            _mockWarehouseRepo.Object,
            _mockProductRepo.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
            _mockCodeGenerator.Object,
            _mockCurrentUser.Object,
            _validator);
    }

    private void SetupPoGetAsync(PurchaseOrder? po)
    {
        _mockPoRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<PurchaseOrder, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<PurchaseOrder>, IQueryable<PurchaseOrder>>?>()))
            .ReturnsAsync(po);
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

    private static PurchaseOrder MakePo(PoStatus status = PoStatus.Draft, int warehouseId = 100, int createdBy = 5, int? submittedBy = null) =>
        new()
        {
            Id = 1,
            PoNumber = "PO-001",
            SupplierName = "Acme Supplies",
            WarehouseId = warehouseId,
            ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Status = status,
            CreatedBy = createdBy,
            SubmittedBy = submittedBy,
            Items = []
        };

    private static Product MakeProduct(int id = 1, EntityStatus status = EntityStatus.Active, decimal unitPrice = 10) =>
        new() { Id = id, Sku = $"PRD-{id:000}", Name = $"Product {id}", CategoryId = 1, UomId = 1, UnitPrice = unitPrice, ReorderLevel = 0, Status = status };

    [Fact]
    public async Task CreatePo_GivenNoWarehouseAssigned_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns((int?)null);

        var request = new PoCreateRequest { SupplierName = "Acme", ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow), ItemList = [] };
        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task CreatePo_GivenWarehouseNotFound_ReturnsNotFound()
    {
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync((Warehouse?)null);

        var request = new PoCreateRequest { SupplierName = "Acme", ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow), ItemList = [] };
        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreatePo_GivenInactiveWarehouse_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Inactive });

        var request = new PoCreateRequest { SupplierName = "Acme", ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow), ItemList = [] };
        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreatePo_GivenDuplicateProductsInItemList_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Active });

        var request = new PoCreateRequest
        {
            SupplierName = "Acme",
            ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow),
            ItemList = [new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 }, new PoItemCreateRequest { ProductId = 1, OrderedQty = 3 }]
        };

        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Duplicate products", result.Message);
    }

    [Fact]
    public async Task CreatePo_GivenItemFailsValidation_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Active });
        _validator.RuleFor(x => x.OrderedQty).Must(_ => false).WithMessage("Forced validation failure for this test.");

        var request = new PoCreateRequest
        {
            SupplierName = "Acme",
            ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow),
            ItemList = [new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 }]
        };

        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Forced validation failure", result.Message);
    }

    [Fact]
    public async Task CreatePo_GivenProductNotFound_ReturnsNotFound()
    {
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Active });
        SetupProductGetAsync(null);

        var request = new PoCreateRequest
        {
            SupplierName = "Acme",
            ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow),
            ItemList = [new PoItemCreateRequest { ProductId = 99, OrderedQty = 5 }]
        };

        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreatePo_GivenInactiveProduct_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Active });
        SetupProductGetAsync(MakeProduct(status: EntityStatus.Inactive));

        var request = new PoCreateRequest
        {
            SupplierName = "Acme",
            ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow),
            ItemList = [new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 }]
        };

        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive", result.Message);
    }

    [Fact]
    public async Task CreatePo_GivenValidRequest_CreatesPoWithItemsAndReturnsSuccess()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Active });
        SetupProductGetAsync(MakeProduct(unitPrice: 10));

        var createdPo = MakePo();
        _mockMapper.Setup(x => x.Map<PurchaseOrder>(It.IsAny<PoCreateRequest>())).Returns(new PurchaseOrder { PoNumber = "TEMP", SupplierName = "Acme", ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow) });
        _mockPoRepo.Setup(x => x.CreateAsync(It.IsAny<PurchaseOrder>())).ReturnsAsync(createdPo);
        _mockCodeGenerator.Setup(x => x.GenerateCode("purchaseorder", 1)).Returns("PO-001");
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        SetupPoGetAsync(createdPo);
        _mockMapper.Setup(x => x.Map<PoResponse>(It.IsAny<PurchaseOrder>())).Returns(new PoResponse { Id = 1, PoNumber = "PO-001", SupplierName = "Acme" });

        var request = new PoCreateRequest
        {
            SupplierName = "Acme",
            ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow),
            ItemList = [new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 }]
        };

        var result = await _service.CreatePo(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        _mockPoItemRepo.Verify(x => x.CreateAsync(It.IsAny<PurchaseOrderItem>()), Times.Once);
        _mockPoRepo.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePo_GivenUnexpectedErrorDuringCreation_RollsBackAndReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        _mockWarehouseRepo
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Warehouse, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<Warehouse>, IQueryable<Warehouse>>?>()))
            .ReturnsAsync(new Warehouse { Id = 100, Code = "WH-100", Name = "Main", Address = "A", City = "C", ContactPerson = "P", ContactPhone = "9999999999", Status = EntityStatus.Active });
        SetupProductGetAsync(MakeProduct());
        _mockMapper.Setup(x => x.Map<PurchaseOrder>(It.IsAny<PoCreateRequest>())).Returns(new PurchaseOrder { PoNumber = "TEMP", SupplierName = "Acme", ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow) });
        _mockPoRepo.Setup(x => x.CreateAsync(It.IsAny<PurchaseOrder>())).ThrowsAsync(new Exception("DB error"));

        var request = new PoCreateRequest
        {
            SupplierName = "Acme",
            ExpectedDelivery = DateOnly.FromDateTime(DateTime.UtcNow),
            ItemList = [new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 }]
        };

        var result = await _service.CreatePo(request);

        Assert.False(result.IsSuccess);
        _mockPoRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }


    [Fact]
    public async Task GetPoById_GivenNotFound_ReturnsNotFound()
    {
        SetupPoGetAsync(null);

        var result = await _service.GetPoById(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetPoById_GivenDraftNotOwnedByCurrentUser_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Draft, createdBy: 99); // created by someone else
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("StockKeeper");
        SetupPoGetAsync(po);

        var result = await _service.GetPoById(1);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetPoById_GivenWarehouseManagerFromDifferentWarehouse_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("WarehouseManager");
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(200); // different warehouse
        SetupPoGetAsync(po);

        var result = await _service.GetPoById(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task GetPoById_GivenValidAccess_ReturnsSuccessWithCorrectFlags()
    {
        var po = MakePo(PoStatus.Draft, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("Administrator");
        SetupPoGetAsync(po);
        _mockMapper.Setup(x => x.Map<PoResponse>(po)).Returns(new PoResponse { Id = 1, PoNumber = "PO-001", SupplierName = "Acme" });

        var result = await _service.GetPoById(1);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.CanEdit); 
    }


    [Fact]
    public async Task GetPos_GivenWarehouseManagerWithNoWarehouseAssigned_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("WarehouseManager");
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns((int?)null);

        var result = await _service.GetPos(new QueryParameters());

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task GetPos_GivenValidQuery_ReturnsPagedResult()
    {
        var po = MakePo();
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("Administrator");
        _mockPoRepo
            .Setup(x => x.GetPaginatedAsync(It.IsAny<QueryParameters>(), It.IsAny<string[]>(), It.IsAny<Func<IQueryable<PurchaseOrder>, IQueryable<PurchaseOrder>>?>()))
            .ReturnsAsync(new PagedResult<PurchaseOrder> { Items = [po], TotalCount = 1, PageNumber = 1, PageSize = 10 });
        _mockMapper.Setup(x => x.Map<PoResponse>(po)).Returns(new PoResponse { Id = 1, PoNumber = "PO-001", SupplierName = "Acme" });

        var result = await _service.GetPos(new QueryParameters());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }


    [Fact]
    public async Task UpdatePo_GivenNotFound_ReturnsNotFound()
    {
        SetupPoGetAsync(null);

        var result = await _service.UpdatePo(1, new PoUpdateRequest { SupplierName = "New" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePo_GivenDifferentWarehouse_ReturnsFailure()
    {
        var po = MakePo(warehouseId: 100);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(200);
        SetupPoGetAsync(po);

        var result = await _service.UpdatePo(1, new PoUpdateRequest { SupplierName = "New" });

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePo_GivenNotOwner_ReturnsFailure()
    {
        var po = MakePo(warehouseId: 100, createdBy: 99);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.UpdatePo(1, new PoUpdateRequest { SupplierName = "New" });

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePo_GivenNotDraft_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.UpdatePo(1, new PoUpdateRequest { SupplierName = "New" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Only draft", result.Message);
    }

    [Fact]
    public async Task UpdatePo_GivenValidPartialChanges_UpdatesOnlyProvidedFieldsAndReturnsSuccess()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Notes = "Old notes";

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<PoResponse>(po)).Returns(new PoResponse { Id = 1, PoNumber = "PO-001", SupplierName = "New Supplier" });

        var result = await _service.UpdatePo(1, new PoUpdateRequest { SupplierName = "New Supplier" });

        Assert.True(result.IsSuccess);
        Assert.Equal("New Supplier", po.SupplierName);
        Assert.Equal("Old notes", po.Notes); 
    }


    [Fact]
    public async Task AddItem_GivenNotFound_ReturnsNotFound()
    {
        SetupPoGetAsync(null);

        var result = await _service.AddItem(1, new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task AddItem_GivenNotDraft_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.AddItem(1, new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task AddItem_GivenProductNotFound_ReturnsNotFound()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        SetupProductGetAsync(null);

        var result = await _service.AddItem(1, new PoItemCreateRequest { ProductId = 99, OrderedQty = 5 });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task AddItem_GivenInactiveProduct_ReturnsFailure()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        SetupProductGetAsync(MakeProduct(status: EntityStatus.Inactive));

        var result = await _service.AddItem(1, new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 });

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive", result.Message);
    }

    [Fact]
    public async Task AddItem_GivenDuplicateProductAlreadyOnPo_ReturnsFailure()
    {
        var existingItem = new PurchaseOrderItem { Id = 1, PoId = 1, ProductId = 1, OrderedQty = 2, UnitPrice = 10, LineTotal = 20 };
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Items = [existingItem];

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        SetupProductGetAsync(MakeProduct());

        var result = await _service.AddItem(1, new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 });

        Assert.False(result.IsSuccess);
        Assert.Contains("already on the purchase order", result.Message);
    }

    [Fact]
    public async Task AddItem_GivenValidRequest_AddsItemRecalculatesTotalAndReturnsSuccess()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.TotalAmount = 0;

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        SetupProductGetAsync(MakeProduct(unitPrice: 10));
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<PoItemResponse>(It.IsAny<PurchaseOrderItem>())).Returns(new PoItemResponse { OrderedQty = 5, UnitPrice = 10, LineTotal = 50 });

        var result = await _service.AddItem(1, new PoItemCreateRequest { ProductId = 1, OrderedQty = 5 });

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(50, po.TotalAmount); // 5 * 10
        _mockPoItemRepo.Verify(x => x.CreateAsync(It.IsAny<PurchaseOrderItem>()), Times.Once);
    }


    [Fact]
    public async Task UpdateItem_GivenPoNotFound_ReturnsNotFound()
    {
        SetupPoGetAsync(null);

        var result = await _service.UpdateItem(1, 1, new PoItemUpdateRequest { OrderedQty = 10 });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_GivenNotDraft_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.UpdateItem(1, 1, new PoItemUpdateRequest { OrderedQty = 10 });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_GivenItemNotFoundOnPo_ReturnsNotFound()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Items = [];

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.UpdateItem(1, 999, new PoItemUpdateRequest { OrderedQty = 10 });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_GivenValidRequest_UpdatesQuantityRecalculatesTotalsAndReturnsSuccess()
    {
        var item = new PurchaseOrderItem { Id = 1, PoId = 1, ProductId = 1, OrderedQty = 5, UnitPrice = 10, LineTotal = 50 };
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Items = [item];

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        SetupProductGetAsync(MakeProduct());
        _mockMapper.Setup(x => x.Map<PoItemResponse>(item)).Returns(new PoItemResponse { OrderedQty = 8, UnitPrice = 10, LineTotal = 80 });

        var result = await _service.UpdateItem(1, 1, new PoItemUpdateRequest { OrderedQty = 8 });

        Assert.True(result.IsSuccess);
        Assert.Equal(8, item.OrderedQty);
        Assert.Equal(80, item.LineTotal);   
        Assert.Equal(80, po.TotalAmount);    
    }


    [Fact]
    public async Task RemoveItem_GivenPoNotFound_ReturnsNotFound()
    {
        SetupPoGetAsync(null);

        var result = await _service.RemoveItem(1, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_GivenNotDraft_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.RemoveItem(1, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_GivenItemNotFoundOnPo_ReturnsNotFound()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Items = [];

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.RemoveItem(1, 999);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_GivenValidRequest_RemovesItemRecalculatesTotalAndReturnsSuccess()
    {
        var item1 = new PurchaseOrderItem { Id = 1, PoId = 1, ProductId = 1, OrderedQty = 5, UnitPrice = 10, LineTotal = 50 };
        var item2 = new PurchaseOrderItem { Id = 2, PoId = 1, ProductId = 2, OrderedQty = 2, UnitPrice = 20, LineTotal = 40 };
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Items = [item1, item2];
        po.TotalAmount = 90;

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.RemoveItem(1, 1);

        Assert.True(result.IsSuccess);
        _mockPoItemRepo.Verify(x => x.DeleteAsync(item1), Times.Once);
        Assert.Equal(40, po.TotalAmount); 
    }


    [Fact]
    public async Task UpdateStatus_Submit_GivenNotFound_ReturnsNotFound()
    {
        SetupPoGetAsync(null);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Submitted });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_Submit_GivenNoItems_ReturnsFailure()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Items = [];

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Submitted });

        Assert.False(result.IsSuccess);
        Assert.Contains("no items", result.Message);
    }

    [Fact]
    public async Task UpdateStatus_Submit_GivenValidDraftWithItems_SubmitsSuccessfully()
    {
        var po = MakePo(warehouseId: 100, createdBy: 5);
        po.Items = [new PurchaseOrderItem { Id = 1, PoId = 1, ProductId = 1, OrderedQty = 5, UnitPrice = 10, LineTotal = 50 }];

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Submitted });

        Assert.True(result.IsSuccess);
        Assert.Equal(PoStatus.Submitted, po.Status);
        Assert.Equal(5, po.SubmittedBy);
    }


    [Fact]
    public async Task UpdateStatus_Approve_GivenNotSubmittedStatus_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Draft, warehouseId: 100);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(7);
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("Administrator");
        SetupPoGetAsync(po);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Approved });

        Assert.False(result.IsSuccess);
        Assert.Contains("Only submitted", result.Message);
    }

    [Fact]
    public async Task UpdateStatus_Approve_GivenApproverIsSubmitter_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, submittedBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5); 
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("Administrator");
        SetupPoGetAsync(po);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Approved });

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot approve", result.Message);
    }

    [Fact]
    public async Task UpdateStatus_Approve_GivenValidRequest_ApprovesSuccessfully()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, submittedBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(7); 
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("Administrator");
        SetupPoGetAsync(po);
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Approved });

        Assert.True(result.IsSuccess);
        Assert.Equal(PoStatus.Approved, po.Status);
        Assert.Equal(7, po.ApprovedBy);
    }


    [Fact]
    public async Task UpdateStatus_Reject_GivenValidRequest_SetsReasonAndReturnsSuccess()
    {
        var po = MakePo(PoStatus.Submitted, warehouseId: 100, submittedBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(7);
        _mockCurrentUser.Setup(x => x.GetUserRole()).Returns("Administrator");
        SetupPoGetAsync(po);
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Rejected, RejectionReason = "Price too high" });

        Assert.True(result.IsSuccess);
        Assert.Equal(PoStatus.Rejected, po.Status);
        Assert.Equal("Price too high", po.RejectionReason);
    }


    [Fact]
    public async Task UpdateStatus_Cancel_GivenAlreadyApproved_ReturnsFailure()
    {
        var po = MakePo(PoStatus.Approved, warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Cancelled });

        Assert.False(result.IsSuccess);
        Assert.Contains("draft & submitted", result.Message);
    }

    [Fact]
    public async Task UpdateStatus_Cancel_GivenDraftPo_CancelsSuccessfully()
    {
        var po = MakePo(PoStatus.Draft, warehouseId: 100, createdBy: 5);
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockCurrentUser.Setup(x => x.GetWarehouseId()).Returns(100);
        SetupPoGetAsync(po);
        _mockPoRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.UpdateStatus(1, new PoStatusUpdateRequest { Status = PoStatus.Cancelled });

        Assert.True(result.IsSuccess);
        Assert.Equal(PoStatus.Cancelled, po.Status);
        Assert.Equal(5, po.CancelledBy);
    }
}