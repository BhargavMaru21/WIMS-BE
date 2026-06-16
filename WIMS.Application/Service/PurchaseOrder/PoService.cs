using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.PurchaseOrder;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;

namespace WIMS.Application.Service;

public class PoService : IPoService
{
    private readonly IPoRepository _poRepository;
    private readonly IPoItemRepository _poItemRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly ICodeGeneratorService _codeGeneratorService;
    private readonly ICurrentUserService _currentUser;

    public PoService(
        IPoRepository poRepository,
        IPoItemRepository poItemRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        IMapper mapper,
        IInputNormalizer inputNormalizer,
        ICodeGeneratorService codeGeneratorService,
        ICurrentUserService currentUser
        )
    {
        _poRepository = poRepository;
        _poItemRepository = poItemRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
        _codeGeneratorService = codeGeneratorService;
        _currentUser = currentUser;
    }

    private void UpdateFlags(PoResponse response, PurchaseOrder po, int currentUserId)
    {
        response.CanEdit = po.Status == PoStatus.Draft;
        response.CanApprove = po.Status == PoStatus.Submitted && po.SubmittedBy != currentUserId;
    }

    public async Task<ApiResponse<PoResponse>> CreatePo(PoCreateRequest request)
    {
        request = _inputNormalizer.NormalizeObject(request);
        int createdByUserId = _currentUser.GetUserId();
        int? warehouseId = _currentUser.GetWarehouseId();


        if (warehouseId is null || warehouseId <= 0)
            return ApiResponse<PoResponse>.Failure("You are not assigned to any warehouse.", statusCode: 403);

        var warehouse = await _warehouseRepository.GetAsync(w => w.Id == warehouseId);

        if (warehouse is null)
            return ApiResponse<PoResponse>.Failure("Warehouse not found.", statusCode: 404);

        if (warehouse.Status == EntityStatus.Inactive)
            return ApiResponse<PoResponse>.Failure("Cannot create a purchase order for an inactive warehouse.", statusCode: 400);


        var entity = _mapper.Map<PurchaseOrder>(request);
        entity.WarehouseId = warehouseId.Value;
        entity.Status = PoStatus.Draft;
        entity.CreatedBy = createdByUserId;

        var created = await _poRepository.CreateAsync(entity);

        created.PoNumber = _codeGeneratorService.GenerateCode("purchaseorder", created.Id);
        await _poRepository.SaveChangesAsync();

        var poWithIncludes = await _poRepository.GetAsync(
            x => x.Id == created.Id,
            includes: q => q
            .Include(p => p.Warehouse)
            .Include(p => p.SubmittedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.RejectedByUser)
            .Include(p => p.CancelledByUser)
            .Include(p => p.Items));

        var response = _mapper.Map<PoResponse>(poWithIncludes);
        UpdateFlags(response, poWithIncludes!, createdByUserId);

        await _poRepository.CommitTransactionAsync();
        return ApiResponse<PoResponse>.Success(response, "Purchase order created successfully.", statusCode: 201);
    }

    public async Task<ApiResponse<PoResponse>> GetPoById(int id)
    {
        int currentUserId = _currentUser.GetUserId();
        string currentUserRole = _currentUser.GetUserRole();
        int? managerWarehouseId = _currentUser.GetWarehouseId();

        var po = await _poRepository.GetAsync(x => x.Id == id, includes: q => q
            .Include(p => p.Warehouse)
            .Include(p => p.SubmittedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.RejectedByUser)
            .Include(p => p.CancelledByUser)
            .Include(p => p.Items));

        if (po is null)
            return ApiResponse<PoResponse>.Failure("Purchase order not found.", statusCode: 404);

        if (po.Status == PoStatus.Draft && po.CreatedBy != currentUserId)
            return ApiResponse<PoResponse>.Failure("This Draft purchase order not belog to you");

        if (currentUserRole == "WarehouseManager" && po.WarehouseId != managerWarehouseId)
            return ApiResponse<PoResponse>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        var response = _mapper.Map<PoResponse>(po);
        UpdateFlags(response, po, currentUserId);

        return ApiResponse<PoResponse>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<PagedResult<PoResponse>>> GetPos(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);
        int currentUserId = _currentUser.GetUserId();
        string currentUserRole = _currentUser.GetUserRole();
        int? managerWarehouseId = _currentUser.GetWarehouseId();

        if (currentUserRole == "WarehouseManager")
        {
            if (managerWarehouseId is null || managerWarehouseId <= 0)
                return ApiResponse<PagedResult<PoResponse>>.Failure("You are not assigned to any warehouse.", statusCode: 403);

            qp.Filters["WarehouseId"] = managerWarehouseId.Value.ToString();

        }

        var paged = await _poRepository.GetPaginatedAsync(
            qp,
            searchableColumns: ["PoNumber", "SupplierName"],
            includes: q => q
            .Include(p => p.Warehouse)
            .Include(p => p.SubmittedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.RejectedByUser)
            .Include(p => p.CancelledByUser)
            .Include(p => p.Items)
        );

        var items = paged.Items.Select(po =>
        {
            var summary = _mapper.Map<PoResponse>(po);
            UpdateFlags(summary, po, currentUserId);
            return summary;
        }).ToList();

        var FilteredItem = items.Where(x => x.Status != PoStatus.Draft.ToString() || (x.Status == PoStatus.Draft.ToString() && x.CreatedBy == currentUserId)).ToList();

        var result = new PagedResult<PoResponse>
        {
            Items = FilteredItem,
            TotalCount = FilteredItem.Count(),
            PageSize = paged.PageSize,
            PageNumber = paged.PageNumber
        };

        return ApiResponse<PagedResult<PoResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<PoResponse>> UpdatePo(int id, PoUpdateRequest request)
    {
        request = _inputNormalizer.NormalizeObject(request);
        int currentUserId = _currentUser.GetUserId();
        int? managerWarehouseId = _currentUser.GetWarehouseId();


        var po = await _poRepository.GetAsync(x => x.Id == id, useNoTracking: false, includes: q => q
            .Include(p => p.Warehouse)
            .Include(p => p.SubmittedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.RejectedByUser)
            .Include(p => p.CancelledByUser)
            .Include(p => p.Items));

        if (po is null)
            return ApiResponse<PoResponse>.Failure("Purchase order not found.", statusCode: 404);

        if (po.WarehouseId != managerWarehouseId)
            return ApiResponse<PoResponse>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.CreatedBy != currentUserId)
            return ApiResponse<PoResponse>.Failure("This purchase order is not belong to your account", statusCode: 403);

        if (po.Status != PoStatus.Draft)
            return ApiResponse<PoResponse>.Failure("Only draft purchase orders can be edited.", statusCode: 400);

        po.SupplierName = !string.IsNullOrWhiteSpace(request.SupplierName) ? request.SupplierName : po.SupplierName;
        po.SupplierContact = request.SupplierContact is not null ? request.SupplierContact : po.SupplierContact;
        po.ExpectedDelivery = request.ExpectedDelivery ?? po.ExpectedDelivery;
        po.Notes = request.Notes is not null ? request.Notes : po.Notes;
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;

        await _poRepository.SaveChangesAsync();

        var updated = await _poRepository.GetAsync(x => x.Id == id, includes: q => q
            .Include(p => p.Warehouse)
            .Include(p => p.SubmittedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.RejectedByUser)
            .Include(p => p.CancelledByUser)
            .Include(p => p.Items));

        var response = _mapper.Map<PoResponse>(updated);
        UpdateFlags(response, updated!, currentUserId);

        return ApiResponse<PoResponse>.Success(response, "Purchase order updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<PoItemResponse>> AddItem(int poId, PoItemCreateRequest request)
    {
        int currentUserId = _currentUser.GetUserId();
        int? managerWarehouseId = _currentUser.GetWarehouseId();
        var po = await _poRepository.GetAsync(x => x.Id == poId, useNoTracking: false, includes: q => q.Include(p => p.Items));

        if (po is null)
            return ApiResponse<PoItemResponse>.Failure("Purchase order not found.", statusCode: 404);

        if (po.WarehouseId != managerWarehouseId)
            return ApiResponse<PoItemResponse>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.CreatedBy != currentUserId)
            return ApiResponse<PoItemResponse>.Failure("This purchase order is not belong to your account", statusCode: 403);

        if (po.Status != PoStatus.Draft)
            return ApiResponse<PoItemResponse>.Failure("Items can only be added while the purchase order is in Draft status.", statusCode: 400);

        var product = await _productRepository.GetAsync(p => p.Id == request.ProductId);
        if (product is null)
            return ApiResponse<PoItemResponse>.Failure("Product not found.", statusCode: 404);

        if (product.Status == EntityStatus.Inactive)
            return ApiResponse<PoItemResponse>.Failure("Cannot add an inactive product to a purchase order.", statusCode: 400);

        if (po.Items.Any(i => i.ProductId == request.ProductId))
            return ApiResponse<PoItemResponse>.Failure("This product is already on the purchase order. Update the existing line instead of adding it again.", statusCode: 400);

        var item = new PurchaseOrderItem
        {
            PoId = po.Id,
            ProductId = request.ProductId,
            OrderedQty = request.OrderedQty,
            UnitPrice = product.UnitPrice,
            ReceivedQty = 0,
            LineTotal = request.OrderedQty * product.UnitPrice,
            CreatedBy = currentUserId
        };

        await _poItemRepository.CreateAsync(item);

        po.TotalAmount = po.TotalAmount + item.LineTotal;
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;
        await _poRepository.SaveChangesAsync();

        var response = _mapper.Map<PoItemResponse>(item);
        response.ProductName = product.Name;

        return ApiResponse<PoItemResponse>.Success(response, "Item added to purchase order successfully.", statusCode: 201);
    }

    public async Task<ApiResponse<PoItemResponse>> UpdateItem(int poId, int itemId, PoItemUpdateRequest request)
    {
        int currentUserId = _currentUser.GetUserId();
        int? managerWarehouseId = _currentUser.GetWarehouseId();
        var po = await _poRepository.GetAsync(x => x.Id == poId, useNoTracking: false, includes: q => q.Include(p => p.Items));

        if (po is null)
            return ApiResponse<PoItemResponse>.Failure("Purchase order not found.", statusCode: 404);

        if (po.WarehouseId != managerWarehouseId)
            return ApiResponse<PoItemResponse>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.CreatedBy != currentUserId)
            return ApiResponse<PoItemResponse>.Failure("This purchase order is not belong to your account", statusCode: 403);

        if (po.Status != PoStatus.Draft)
            return ApiResponse<PoItemResponse>.Failure("Items can only be edited while the purchase order is in Draft status.", statusCode: 400);

        var item = po.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return ApiResponse<PoItemResponse>.Failure("Purchase order item not found.", statusCode: 404);

        item.OrderedQty = request.OrderedQty ?? item.OrderedQty;
        item.LineTotal = item.OrderedQty * item.UnitPrice;
        item.ModifiedBy = currentUserId;
        item.ModifiedAt = DateTime.UtcNow;

        po.TotalAmount = po.Items.Sum(i => i.LineTotal);
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;

        await _poRepository.SaveChangesAsync();

        var product = await _productRepository.GetAsync(p => p.Id == item.ProductId);

        var response = _mapper.Map<PoItemResponse>(item);
        response.ProductName = product!.Name;

        return ApiResponse<PoItemResponse>.Success(response, "Purchase order item updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> RemoveItem(int poId, int itemId)
    {
        int currentUserId = _currentUser.GetUserId();
        int? managerWarehouseId = _currentUser.GetWarehouseId();
        var po = await _poRepository.GetAsync(x => x.Id == poId, useNoTracking: false, includes: q => q.Include(p => p.Items));

        if (po is null)
            return ApiResponse<string>.Failure("Purchase order not found.", statusCode: 404);

        if (po.WarehouseId != managerWarehouseId)
            return ApiResponse<string>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.CreatedBy != currentUserId)
            return ApiResponse<string>.Failure("This purchase order is not belong to your account", statusCode: 403);

        if (po.Status != PoStatus.Draft)
            return ApiResponse<string>.Failure("Items can only be removed while the purchase order is in Draft status.", statusCode: 400);

        var item = po.Items.FirstOrDefault(i => i.Id == itemId);

        if (item is null)
            return ApiResponse<string>.Failure("Purchase order item not found.", statusCode: 404);

        await _poItemRepository.DeleteAsync(item);

        po.TotalAmount = po.Items.Where(i => i.Id != itemId).Sum(i => i.LineTotal);
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;
        await _poRepository.SaveChangesAsync();

        return ApiResponse<string>.Success("Item removed from purchase order successfully.", statusCode: 200);
    }   

    public async Task<ApiResponse<string>> SubmitPo(int id)
    {
        int currentUserId = _currentUser.GetUserId();
        int? managerWarehouseId = _currentUser.GetWarehouseId();
        var po = await _poRepository.GetAsync(x => x.Id == id, useNoTracking: false, includes: q => q.Include(p => p.Items));

        if (po is null)
            return ApiResponse<string>.Failure("Purchase order not found.", statusCode: 404);

        if (po.WarehouseId != managerWarehouseId)
            return ApiResponse<string>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.CreatedBy != currentUserId)
            return ApiResponse<string>.Failure("This Purchase Order is not belong to your account", statusCode: 403);

        if (po.Status != PoStatus.Draft)
            return ApiResponse<string>.Failure("Only draft purchase orders can be submitted.", statusCode: 400);

        if (po.Items.Count == 0)
            return ApiResponse<string>.Failure("Cannot submit a purchase order with no items.", statusCode: 400);

        po.Status = PoStatus.Submitted;
        po.OrderDate = DateOnly.FromDateTime(DateTime.UtcNow);
        po.SubmittedBy = currentUserId;
        po.SubmittedAt = DateTime.UtcNow;
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;

        await _poRepository.SaveChangesAsync();

        return ApiResponse<string>.Success("Purchase order submitted for approval successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> ApprovePo(int id)
    {
        int currentUserId = _currentUser.GetUserId();
        string currentUserRole = _currentUser.GetUserRole();
        int? managerWarehouseId = _currentUser.GetWarehouseId();
        var po = await _poRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (po is null)
            return ApiResponse<string>.Failure("Purchase order not found.", statusCode: 404);

        if (currentUserRole == "WarehouseManager" && po.WarehouseId != managerWarehouseId)
            return ApiResponse<string>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.Status != PoStatus.Submitted)
            return ApiResponse<string>.Failure("Only submitted purchase orders can be approved.", statusCode: 400);

        if (po.SubmittedBy == currentUserId)
            return ApiResponse<string>.Failure("You cannot approve a purchase order that you submitted. Another manager or administrator must approve it.", statusCode: 403);

        po.Status = PoStatus.Approved;
        po.ApprovedBy = currentUserId;
        po.ApprovedAt = DateTime.UtcNow;
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;

        await _poRepository.SaveChangesAsync();

        return ApiResponse<string>.Success("Purchase order approved successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> RejectPo(int id, PoRejectRequest request)
    {
        int currentUserId = _currentUser.GetUserId();
        string currentUserRole = _currentUser.GetUserRole();
        int? managerWarehouseId = _currentUser.GetWarehouseId();
        request = _inputNormalizer.NormalizeObject(request);

        var po = await _poRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (po is null)
            return ApiResponse<string>.Failure("Purchase order not found.", statusCode: 404);

        if (currentUserRole == "WarehouseManager" && po.WarehouseId != managerWarehouseId)
            return ApiResponse<string>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.Status != PoStatus.Submitted)
            return ApiResponse<string>.Failure("Only submitted purchase orders can be rejected.", statusCode: 400);

        if (po.SubmittedBy == currentUserId)
            return ApiResponse<string>.Failure("You cannot reject a purchase order that you submitted . Another manager or administrator must reveiw it.", statusCode: 403);

        po.Status = PoStatus.Rejected;
        po.RejectedBy = currentUserId;
        po.RejectedAt = DateTime.UtcNow;
        po.RejectionReason = request.RejectionReason;
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;

        await _poRepository.SaveChangesAsync();

        return ApiResponse<string>.Success("Purchase order rejected successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> CancelPo(int id)
    {
        int currentUserId = _currentUser.GetUserId();
        int? managerWarehouseId = _currentUser.GetWarehouseId();
        var po = await _poRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (po is null)
            return ApiResponse<string>.Failure("Purchase order not found.", statusCode: 404);

        if (po.WarehouseId != managerWarehouseId)
            return ApiResponse<string>.Failure("This purchase order does not belong to your warehouse.", statusCode: 403);

        if (po.CreatedBy != currentUserId)
            return ApiResponse<string>.Failure("This purchase order is not belong to your account", statusCode: 403);

        if (po.Status != PoStatus.Draft)
            return ApiResponse<string>.Failure("Only draft purchase orders can be cancelled.", statusCode: 400);

        po.Status = PoStatus.Cancelled;
        po.CancelledBy = currentUserId;
        po.CancelledAt = DateTime.UtcNow;
        po.ModifiedBy = currentUserId;
        po.ModifiedAt = DateTime.UtcNow;

        await _poRepository.SaveChangesAsync();

        return ApiResponse<string>.Success("Purchase order cancelled successfully.", statusCode: 200);
    }
}