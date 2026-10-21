using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Warehouse;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.Audit;
using WIMS.Application.Interfaces.Services.WarehouseManagement;
using WIMS.Domain.Constant;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;

namespace WIMS.Application.Service.WarehouseManagement;

public class ZoneService : IZoneService
{
    private readonly IZoneRepository _zoneRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IBinRepository _binRepository;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly ICodeGeneratorService _codeGeneratorService;
    private readonly ICurrentUserService _currentUser;
    public ZoneService(
        IZoneRepository zoneRepository,
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        IInputNormalizer inputNormalizer,
        ICodeGeneratorService codeGeneratorService,
        IBinRepository binRepository,
        ICurrentUserService currentUser
        )
    {
        _zoneRepository = zoneRepository;
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
        _codeGeneratorService = codeGeneratorService;
        _binRepository = binRepository;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<ZoneResponse>> CreateZone(ZoneCreateRequest request)
    {
        int createdByUserId = _currentUser.GetUserId();
        request = _inputNormalizer.NormalizeObject(request);

        var warehouse = await _warehouseRepository.GetAsync(w => w.Id == request.WarehouseId);
        if (warehouse is null)
            return ApiResponse<ZoneResponse>.Failure("Warehouse not found.", statusCode: 404);

        if (warehouse.Status == EntityStatus.Inactive)
            return ApiResponse<ZoneResponse>.Failure("Cannot add a zone to an inactive warehouse.", statusCode: 400);

        bool nameExists = await _zoneRepository.ExistsAsync(
            z => z.WarehouseId == request.WarehouseId &&
                 z.Name.ToLower() == request.Name.ToLower());

        if (nameExists)
            return ApiResponse<ZoneResponse>.Failure(
                "A zone with this name already exists in the selected warehouse.", statusCode: 400);

        var zoneEntity = _mapper.Map<Zone>(request);
        zoneEntity.CreatedBy = createdByUserId;

        var createdZone = await _zoneRepository.CreateAsync(zoneEntity);

        createdZone.Code = _codeGeneratorService.GenerateCode("zone", createdZone.Id);
        await _zoneRepository.SaveChangesAsync();

        var zoneWitWarehouse = await _zoneRepository.GetAsync(
            z => z.Id == createdZone.Id,
            includes: q => q.Include(z => z.Warehouse));

        var response = _mapper.Map<ZoneResponse>(zoneWitWarehouse);

        return ApiResponse<ZoneResponse>.Success(response, "Zone created successfully.", statusCode: 201);
    }

    public async Task<ApiResponse<ZoneResponse>> GetZoneById(int id)
    {
        if (id <= 0)
            return ApiResponse<ZoneResponse>.Failure("Invalid Id", statusCode: 400);

        var zone = await _zoneRepository.GetAsync(
            z => z.Id == id,
            includes: q => q.Include(z => z.Warehouse));

        if (zone is null)
            return ApiResponse<ZoneResponse>.Failure("Zone not found.", statusCode: 404);

        var response = _mapper.Map<ZoneResponse>(zone);

        return ApiResponse<ZoneResponse>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<PagedResult<ZoneResponse>>> GetZones(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);

        var pagedZones = await _zoneRepository.GetPaginatedAsync(
            qp,
            searchableColumns: ["Name", "Code"],
            includes: q => q.Include(z => z.Warehouse));

        var result = new PagedResult<ZoneResponse>
        {
            Items = _mapper.Map<List<ZoneResponse>>(pagedZones.Items),
            TotalCount = pagedZones.TotalCount,
            PageSize = pagedZones.PageSize,
            PageNumber = pagedZones.PageNumber
        };

        return ApiResponse<PagedResult<ZoneResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<List<ZoneDropdownResponse>>> GetZonesDropdown(int? warehouseId = null)
    {
        var allZones = await _zoneRepository.GetAllAsync(
            orderBy: q => q.OrderBy(z => z.Name));

        var filtered = allZones
            .Where(z => warehouseId == null || z.WarehouseId == warehouseId)
            .ToList();

        var response = _mapper.Map<List<ZoneDropdownResponse>>(filtered);
        return ApiResponse<List<ZoneDropdownResponse>>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<ZoneResponse>> UpdateZone(int id, ZoneUpdateRequest request)
    {
        if (id <= 0)
            return ApiResponse<ZoneResponse>.Failure("Invalid Id", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();

        request = _inputNormalizer.NormalizeObject(request);

        var zone = await _zoneRepository.GetAsync(
            z => z.Id == id,
            useNoTracking: false,
            includes: q => q.Include(z => z.Warehouse));

        if (zone is null)
            return ApiResponse<ZoneResponse>.Failure("Zone not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(request.Name) &&
            await _zoneRepository.ExistsAsync(
                z => z.WarehouseId == zone.WarehouseId &&
                     z.Name.ToLower() == request.Name.ToLower() &&
                     z.Id != id))
        {
            return ApiResponse<ZoneResponse>.Failure(
                "A zone with this name already exists in the warehouse.", statusCode: 400);
        }

        zone.Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : zone.Name;
        zone.ModifiedBy = modifiedByUserId;
        zone.ModifiedAt = DateTime.UtcNow;

        await _zoneRepository.SaveChangesAsync();
        var response = _mapper.Map<ZoneResponse>(zone);

        return ApiResponse<ZoneResponse>.Success(response, "Zone updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> UpdateZoneStatus(int id, ZoneStatusUpdateRequest request)
    {
        if (id <= 0)
            return ApiResponse<string>.Failure("Invalid Id", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();

        var zone = await _zoneRepository.GetAsync(
            z => z.Id == id,
            useNoTracking: false,
            includes: q => q.Include(z => z.Bins));

        if (zone is null)
            return ApiResponse<string>.Failure("Zone not found.", statusCode: 404);

        if (zone.Status == request.Status)
            return ApiResponse<string>.Failure($"Zone is already {zone.Status}.", statusCode: 400);

        // any bin is active then we can not inactive zone
        if (request.Status == EntityStatus.Inactive && zone.Bins.Any(b => b.Status == EntityStatus.Active))
        {
            return ApiResponse<string>.Failure("Cannot deactivate a zone that has active bins. Please deactivate all bins first.", statusCode: 400);
        }

        //if all bins are inactive then we can not active Zone
        if (request.Status == EntityStatus.Active && zone.Bins.Count > 0 && zone.Bins.All(b => b.Status == EntityStatus.Inactive))
        {
            return ApiResponse<string>.Failure("Cannot activate a zone that has only inactive bins. Please activate at least one bin first.", statusCode: 400);
        }

        await _zoneRepository.BeginTransactionAsync();
        try
        {
            // if inactivating the last active zone in the warehouse, then inactivate the warehouse as well
            if (request.Status == EntityStatus.Inactive && await isLastActiveZoneInWarehouse(zone.WarehouseId))
            {
                zone.Status = request.Status;
                zone.ModifiedBy = modifiedByUserId;
                zone.ModifiedAt = DateTime.UtcNow;

                await _zoneRepository.SaveChangesAsync();
                var warehouseResponse = await UpdateWarehouseStatus(zone.WarehouseId, new WarehouseStatusUpdateRequest { Status = EntityStatus.Inactive }, modifiedByUserId);

                if (!warehouseResponse.IsSuccess)
                {
                    await _zoneRepository.RollbackTransactionAsync();
                    return ApiResponse<string>.Failure("An error occurred while updating the warehouse status.", statusCode: 500);
                }

                await _zoneRepository.CommitTransactionAsync();
                return ApiResponse<string>.Success($"Zone and its warehouse inactivated successfully.", statusCode: 200);
            }


            //if first zone is activating then we need to active warehouse.
            if (request.Status == EntityStatus.Active && await isFirstZoneWillActiveInWarehouse(zone.WarehouseId))
            {
                zone.Status = request.Status;
                zone.ModifiedBy = modifiedByUserId;
                zone.ModifiedAt = DateTime.UtcNow;

                await _zoneRepository.SaveChangesAsync();

                var warehouseResponse = await UpdateWarehouseStatus(zone.WarehouseId, new WarehouseStatusUpdateRequest { Status = EntityStatus.Active }, modifiedByUserId);

                if (!warehouseResponse.IsSuccess)
                {
                    await _zoneRepository.RollbackTransactionAsync();
                    return ApiResponse<string>.Failure("An error occurred while updating the warehouse status.", statusCode: 500);
                }

                await _zoneRepository.CommitTransactionAsync();
                return ApiResponse<string>.Success($"Zone and its warehouse activated successfully.", statusCode: 200);

            }
            zone.Status = request.Status;
            zone.ModifiedBy = modifiedByUserId;
            zone.ModifiedAt = DateTime.UtcNow;

            await _zoneRepository.SaveChangesAsync();

            await _zoneRepository.CommitTransactionAsync();
            return ApiResponse<string>.Success($"Zone {zone.Status} successfully.", statusCode: 200);

        }
        catch (Exception)
        {
            await _zoneRepository.RollbackTransactionAsync();
            return ApiResponse<string>.Failure("An error occurred while updating the zone status.", statusCode: 500);
        }
    }

    private async Task<bool> isLastActiveZoneInWarehouse(int warehouseId)
    {
        var activeZones = await _zoneRepository.GetActiveZoneByWarehouseAsync(warehouseId);
        return activeZones.Count == 1;
    }

    private async Task<bool> isFirstZoneWillActiveInWarehouse(int warehouseId)
    {
        var activeZones = await _zoneRepository.GetActiveZoneByWarehouseAsync(warehouseId);
        return activeZones.Count == 0;
    }

    public async Task<ApiResponse<string>> UpdateWarehouseStatus(int warehouseId, WarehouseStatusUpdateRequest request, int modifiedByUserId)
    {
        var warehouse = await _warehouseRepository.GetAsync(w => w.Id == warehouseId, useNoTracking: false);

        if (warehouse is null)
            return ApiResponse<string>.Failure("Warehouse not found.", statusCode: 404);

        warehouse.Status = request.Status;
        warehouse.ModifiedBy = modifiedByUserId;
        warehouse.ModifiedAt = DateTime.UtcNow;

        await _warehouseRepository.SaveChangesAsync();
        return ApiResponse<string>.Success($"Warehouse {warehouse.Status} successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> DeleteZone(int id)
    {
        if (id <= 0)
            return ApiResponse<string>.Failure("Invalid Id", statusCode: 400);

        int deletedBy = _currentUser.GetUserId();

        var zone = await _zoneRepository.GetAsync(z => z.Id == id, useNoTracking: false, includes: q => q.Include(b => b.Bins));

        if (zone is null)
            return ApiResponse<string>.Failure("Zone not found.", statusCode: 404);

        var allBins = zone.Bins.ToList();

        foreach (var bin in allBins)
        {
            if (await _binRepository.HasStockAsync(bin.Id))
                return ApiResponse<string>.Failure("Cannot delete a zone that has bins with stock. Please remove stock from all bins first.", statusCode: 400);
        }

        await _zoneRepository.BeginTransactionAsync();

        try
        {
            foreach (var bin in allBins)
            {
                await _binRepository.SoftDeleteAsync(bin, deletedBy);
            }

            await _zoneRepository.SoftDeleteAsync(zone, deletedBy);
            await _zoneRepository.CommitTransactionAsync();
            return ApiResponse<string>.Success("Zone and it's related Bins are deleted ");

        }
        catch (Exception)
        {
            await _zoneRepository.RollbackTransactionAsync();
            return ApiResponse<string>.Failure("an error occure while deleting Zone");
        }
    }
}