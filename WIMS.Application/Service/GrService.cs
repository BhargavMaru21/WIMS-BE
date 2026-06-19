using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.GoodsReceipts;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;

namespace WIMS.Application.Service;

public class GrService : IGrService
{
    private readonly IGrRepository _GrRepository;
    private readonly IPoRepository _poRepository;
    private readonly IPoItemRepository _poItemRepository;
    private readonly IBinRepository _binRepository;
    private readonly IStockRecordRepository _stockRecordRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly ICodeGeneratorService _codeGeneratorService;
    private readonly ICurrentUserService _currentUser;

    public GrService(
        IGrRepository GrRepository,
        IPoRepository poRepository,
        IPoItemRepository poItemRepository,
        IBinRepository binRepository,
        IStockRecordRepository stockRecordRepository,
        IStockMovementRepository stockMovementRepository,
        IMapper mapper,
        IInputNormalizer inputNormalizer,
        ICodeGeneratorService codeGeneratorService,
        ICurrentUserService currentUser)
    {
        _GrRepository = GrRepository;
        _poRepository = poRepository;
        _poItemRepository = poItemRepository;
        _binRepository = binRepository;
        _stockRecordRepository = stockRecordRepository;
        _stockMovementRepository = stockMovementRepository;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
        _codeGeneratorService = codeGeneratorService;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<GrResponse>> CreateGr(GrCreateRequest request)
    {
        request = _inputNormalizer.NormalizeObject(request);
        int currentUserId = _currentUser.GetUserId();
        int? stockKeeperWarehouseId = _currentUser.GetWarehouseId();

        if (stockKeeperWarehouseId is null || stockKeeperWarehouseId <= 0)
            return ApiResponse<GrResponse>.Failure("You are not assigned to any warehouse.", statusCode: 403);

        if (request.Items.Count == 0)
            return ApiResponse<GrResponse>.Failure("At least one item must be received.", statusCode: 400);

        if (request.Items.Select(i => i.PoItemId).Distinct().Count() != request.Items.Count)
            return ApiResponse<GrResponse>.Failure("The same purchase order item cannot appear twice in one receipt.", statusCode: 400);

        var po = await _poRepository.GetAsync(x => x.Id == request.PoId, useNoTracking: false, includes: q => q.Include(p => p.Items));

        if (po is null)
            return ApiResponse<GrResponse>.Failure("Purchase order not found.", statusCode: 404);

        if (po.WarehouseId != stockKeeperWarehouseId)
            return ApiResponse<GrResponse>.Failure("purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.Status != PoStatus.Approved && po.Status != PoStatus.PartiallyReceived)
            return ApiResponse<GrResponse>.Failure("Goods can only received against approved purchase order.", statusCode: 400);

        var GrItems = new List<GoodsReceiptItem>();

        foreach (var line in request.Items)
        {
            var poItem = po.Items.FirstOrDefault(i => i.Id == line.PoItemId);

            if (poItem is null)
                return ApiResponse<GrResponse>.Failure($"purchase order item {line.PoItemId} does not belong to this purchase order.", statusCode: 400);

            if (line.Quantity <= 0)
                return ApiResponse<GrResponse>.Failure("Received quantity must be greater than zero.", statusCode: 400);

            var remaining = poItem.OrderedQty - poItem.ReceivedQty;

            if (line.Quantity > remaining)
                return ApiResponse<GrResponse>.Failure($"Received quantity for product {line.ProductId} exced the remaining order quantity ({remaining}).", statusCode: 400);

            var bin = await _binRepository.GetAsync(x => x.Id == line.BinId, includes: q => q.Include(b => b.Zone));

            if (bin is null)
                return ApiResponse<GrResponse>.Failure("Bin not found.", statusCode: 404);

            if (bin.Zone.WarehouseId != po.WarehouseId)
                return ApiResponse<GrResponse>.Failure("Selected bin does not belong to this warehouse.", statusCode: 400);

            if (bin.Status == EntityStatus.Inactive)
                return ApiResponse<GrResponse>.Failure("Cannot add goods into inactive bin.", statusCode: 400);

            GrItems.Add(new GoodsReceiptItem
            {
                PoItemId = poItem.Id,
                ProductId = line.ProductId,
                BinId = line.BinId,
                Quantity = line.Quantity,
                Condition = line.Condition,
                CreatedBy = currentUserId
            });

            poItem.ReceivedQty += line.Quantity;
        }

        var Gr = new GoodsReceipt
        {
            GrnNumber = "TEMP",
            PoId = po.Id,
            WarehouseId = po.WarehouseId,
            ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ReceivedBy = currentUserId,
            Notes = request.Notes,
            CreatedBy = currentUserId,
            Items = GrItems
        };

        await _GrRepository.BeginTransactionAsync();
        try
        {
            await _GrRepository.CreateAsync(Gr);

            Gr.GrnNumber = _codeGeneratorService.GenerateCode("goodsreceipt", Gr.Id);

            var goodConditionItems = GrItems.Where(x => x.Condition == ReceiptCondition.Good);

            foreach (var item in goodConditionItems)
            {
                var stockRecord = await _stockRecordRepository.GetOrCreateAsync(item.ProductId, po.WarehouseId, item.BinId);

                var before = stockRecord.Quantity;
                stockRecord.Quantity += item.Quantity;

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    ProductId = item.ProductId,
                    WarehouseId = po.WarehouseId,
                    BinId = item.BinId,
                    MovementType = StockMovementType.GoodsReceipt,
                    QuantityChange = item.Quantity,
                    QuantityBefore = before,
                    QuantityAfter = stockRecord.Quantity,
                    ReferenceType = nameof(GoodsReceipt),
                    ReferenceId = Gr.Id,
                    PerformedBy = currentUserId
                });
            }

            bool allReceived = po.Items.All(x => x.ReceivedQty == x.OrderedQty);
            bool someReceived = po.Items.Any(x => x.ReceivedQty > 0);

            po.Status = allReceived ? PoStatus.FullyReceived : someReceived ? PoStatus.PartiallyReceived : po.Status;
            po.ModifiedBy = currentUserId;
            po.ModifiedAt = DateTime.UtcNow;

            await _GrRepository.SaveChangesAsync();
            await _GrRepository.CommitTransactionAsync();
        }
        catch
        {
            await _GrRepository.RollbackTransactionAsync();
            throw;
        }

        var GrWithIncludes = await _GrRepository.GetAsync(x => x.Id == Gr.Id,
            includes: q => q
                .Include(x => x.Warehouse)
                .Include(x => x.PurchaseOrder)
                .Include(x => x.ReceivedByUser)
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .Include(x => x.Items).ThenInclude(i => i.Bin));

        var response = _mapper.Map<GrResponse>(GrWithIncludes);

        return ApiResponse<GrResponse>.Success(response, "Goods receipt recorded successfully.", statusCode: 201);
    }

    public async Task<ApiResponse<GrResponse>> GetGrById(int id)
    {
        string currentUserRole = _currentUser.GetUserRole();
        int? userWarehouseId = _currentUser.GetWarehouseId();

        var Gr = await _GrRepository.GetAsync(x => x.Id == id,
            includes: q => q
                .Include(x => x.Warehouse)
                .Include(x => x.PurchaseOrder)
                .Include(x => x.ReceivedByUser)
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .Include(x => x.Items).ThenInclude(i => i.Bin));

        if (Gr is null)
            return ApiResponse<GrResponse>.Failure("Goods receipt not found.", statusCode: 404);

        if (currentUserRole != "Administrator" && Gr.WarehouseId != userWarehouseId)
            return ApiResponse<GrResponse>.Failure("This goods receipt does not belong to your warehouse.", statusCode: 403);

        var response = _mapper.Map<GrResponse>(Gr);
        return ApiResponse<GrResponse>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<PagedResult<GrResponse>>> GetGrs(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);
        string currentUserRole = _currentUser.GetUserRole();
        int? userWarehouseId = _currentUser.GetWarehouseId();

        if (currentUserRole != "Administrator")
        {
            if (userWarehouseId is null || userWarehouseId <= 0)
                return ApiResponse<PagedResult<GrResponse>>.Failure("You are not assigned to any warehouse.", statusCode: 403);

            qp.Filters["WarehouseId"] = userWarehouseId.Value.ToString();
        }

        var paged = await _GrRepository.GetPaginatedAsync(
            qp,
            searchableColumns: ["GrnNumber"],
            includes: q => q
                .Include(x => x.Warehouse)
                .Include(x => x.PurchaseOrder)
                .Include(x => x.ReceivedByUser)
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .Include(x => x.Items).ThenInclude(i => i.Bin));

        var items = paged.Items.Select(g => _mapper.Map<GrResponse>(g)).ToList();

        var result = new PagedResult<GrResponse>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            PageSize = paged.PageSize,
            PageNumber = paged.PageNumber
        };

        return ApiResponse<PagedResult<GrResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<List<PendingPoResponse>>> GetPendingPos()
    {
        int? stockKeeperWarehouseId = _currentUser.GetWarehouseId();

        if (stockKeeperWarehouseId is null || stockKeeperWarehouseId <= 0)
            return ApiResponse<List<PendingPoResponse>>.Failure("You are not assigned to any warehouse.", statusCode: 403);

        var pos = await _poRepository.GetAllAsync(includes: q => q.Include(p => p.Items).ThenInclude(x => x.Product)
                            .Where(p => p.WarehouseId == stockKeeperWarehouseId && (p.Status == PoStatus.Approved || p.Status == PoStatus.PartiallyReceived)));

        var response = pos.Select(po => new PendingPoResponse
        {
            PoId = po.Id,
            PoNumber = po.PoNumber,
            SupplierName = po.SupplierName,
            ExpectedDelivery = po.ExpectedDelivery,
            Status = po.Status.ToString(),
            Items = po.Items.Where(x => x.ReceivedQty < x.OrderedQty)
                .Select(x => new PendingPoItemResponse
                {
                    PoItemId = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    OrderedQty = x.OrderedQty,
                    ReceivedQty = x.ReceivedQty,
                    RemainingQty = x.OrderedQty - x.ReceivedQty
                }).ToList()
        }).ToList();

        return ApiResponse<List<PendingPoResponse>>.Success(response, statusCode: 200);
    }
}
