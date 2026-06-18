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

public class BinService : IBinService
{
    private readonly IBinRepository _binRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly ICodeGeneratorService _codeGeneratorService;
    private readonly ZoneService _zoneService;
    private readonly ICurrentUserService _currentUser;

    public BinService(
        IBinRepository binRepository,
        IZoneRepository zoneRepository,
        IMapper mapper,
        IInputNormalizer inputNormalizer,
        ICodeGeneratorService codeGeneratorService,
        ZoneService zoneService,
        ICurrentUserService currentUser
        )
    {
        _binRepository = binRepository;
        _zoneRepository = zoneRepository;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
        _codeGeneratorService = codeGeneratorService;
        _zoneService = zoneService;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<BinResponse>> CreateBin(BinCreateRequest request)
    {
        int createdByUserId = _currentUser.GetUserId();
        request = _inputNormalizer.NormalizeObject(request);

        var zone = await _zoneRepository.GetAsync(
            z => z.Id == request.ZoneId);

        if (zone is null)
            return ApiResponse<BinResponse>.Failure("Zone not found.", statusCode: 404);

        if (zone.Status == EntityStatus.Inactive)
            return ApiResponse<BinResponse>.Failure("Cannot add a bin to an inactive zone.", statusCode: 400);

        bool nameExists = await _binRepository.ExistsAsync(
            b => b.ZoneId == request.ZoneId &&
                 b.Name.ToLower() == request.Name.ToLower());

        if (nameExists)
            return ApiResponse<BinResponse>.Failure("A bin with this name already exists in the selected zone.", statusCode: 400);

        var binEntity = _mapper.Map<Bin>(request);
        binEntity.CreatedBy = createdByUserId;

        var createdBin = await _binRepository.CreateAsync(binEntity);

        createdBin.Code = _codeGeneratorService.GenerateCode("bin", createdBin.Id);
        await _binRepository.SaveChangesAsync();

        var bin = await _binRepository.GetAsync(
            b => b.Id == createdBin.Id,
            includes: q => q
                .Include(b => b.Zone)
                .ThenInclude(z => z.Warehouse));

        var response = _mapper.Map<BinResponse>(bin);

        return ApiResponse<BinResponse>.Success(response, "Bin created successfully.", statusCode: 201);
    }

    public async Task<ApiResponse<BinResponse>> GetBinById(int id)
    {
        if (id <= 0)
            return ApiResponse<BinResponse>.Failure("Invalid Id", statusCode: 400);

        var bin = await _binRepository.GetAsync(b => b.Id == id, includes: q => q.Include(b => b.Zone).ThenInclude(z => z.Warehouse));

        if (bin is null)
            return ApiResponse<BinResponse>.Failure("Bin not found.", statusCode: 404);

        var response = _mapper.Map<BinResponse>(bin);
        return ApiResponse<BinResponse>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<string>> DeleteBin(int id)
    {
        if (id <= 0)
            return ApiResponse<string>.Failure("Invalid Id", statusCode: 400);

        int deletedBy = _currentUser.GetUserId();
        var bin = await _binRepository.GetAsync(b => b.Id == id, useNoTracking: false);

        if (bin is null)
        {
            return ApiResponse<string>.Failure("Bin not found.", statusCode: 404);
        }

        if (await _binRepository.HasStockAsync(bin.Id))
        {
            return ApiResponse<string>.Failure("Cannot delete a Bin that has stock. Please remove stock  first.");
        }

        await _binRepository.SoftDeleteAsync(bin, deletedBy);
        return ApiResponse<string>.Success("Bin Deleted Successfully.");
    }

    public async Task<ApiResponse<PagedResult<BinResponse>>> GetBins(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);

        var pagedBins = await _binRepository.GetPaginatedAsync(qp, searchableColumns: ["Name", "Code"], includes: q => q.Include(b => b.Zone).ThenInclude(z => z.Warehouse));

        var result = new PagedResult<BinResponse>
        {
            Items = _mapper.Map<List<BinResponse>>(pagedBins.Items),
            TotalCount = pagedBins.TotalCount,
            PageSize = pagedBins.PageSize,
            PageNumber = pagedBins.PageNumber
        };

        return ApiResponse<PagedResult<BinResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<List<BinDropdownResponse>>> GetBinsDropdown(int? warehouseId = null, int? zoneId = null)
    {
        var allBins = await _binRepository.GetAllAsync(
            orderBy: q => q.OrderBy(b => b.Code),
            includes: q => q.Include(b => b.Zone));

        var filtered = allBins
            .Where(b => zoneId == null || b.ZoneId == zoneId)
            .Where(b => warehouseId == null || b.Zone?.WarehouseId == warehouseId)
            .ToList();

        var response = _mapper.Map<List<BinDropdownResponse>>(filtered);
        return ApiResponse<List<BinDropdownResponse>>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<BinResponse>> UpdateBin(int id, BinUpdateRequest request)
    {
        if (id <= 0)
            return ApiResponse<BinResponse>.Failure("Invalid Id", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();

        request = _inputNormalizer.NormalizeObject(request);

        var bin = await _binRepository.GetAsync(b => b.Id == id, useNoTracking: false,
            includes: q => q.Include(b => b.Zone)
                            .ThenInclude(z => z.Warehouse));

        if (bin is null)
            return ApiResponse<BinResponse>.Failure("Bin not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(request.Name) &&
            await _binRepository.ExistsAsync(b => b.ZoneId == bin.ZoneId && b.Name.ToLower() == request.Name.ToLower() && b.Id != id))
        {
            return ApiResponse<BinResponse>.Failure("A bin with this name already exists in the zone.", statusCode: 400);
        }

        bin.Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : bin.Name;

        if (request.MaxCapacity.HasValue)
            bin.MaxCapacity = request.MaxCapacity.Value;

        bin.ModifiedBy = modifiedByUserId;
        bin.ModifiedAt = DateTime.UtcNow;

        await _binRepository.SaveChangesAsync();
        var response = _mapper.Map<BinResponse>(bin);

        return ApiResponse<BinResponse>.Success(response, "Bin updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> UpdateBinStatus(int id, BinStatusUpdateRequest request)
    {
        if (id <= 0)
            return ApiResponse<string>.Failure("Invalid Id", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();

        var bin = await _binRepository.GetAsync(b => b.Id == id, useNoTracking: false);

        if (bin is null)
            return ApiResponse<string>.Failure("Bin not found.", statusCode: 404);

        if (bin.Status == request.Status)
            return ApiResponse<string>.Failure($"Bin is already {bin.Status}.", statusCode: 400);

        if (request.Status == EntityStatus.Inactive && await _binRepository.HasStockAsync(id))
        {
            return ApiResponse<string>.Failure("Cannot deactivate a bin that currently holds stock. Please move or adjust the stock first.", statusCode: 400);
        }

        await _binRepository.BeginTransactionAsync();

        try
        {

            //if last active bin is inactivating then we need to inactive zone.
            if (request.Status == EntityStatus.Inactive && await isLastActiveBinInZone(bin.ZoneId))
            {
                bin.Status = request.Status;
                bin.ModifiedBy = modifiedByUserId;
                bin.ModifiedAt = DateTime.UtcNow;

                await _binRepository.SaveChangesAsync();

                var response = await UpdateZoneStatus(bin.ZoneId, new ZoneStatusUpdateRequest { Status = EntityStatus.Inactive }, modifiedByUserId);

                if (!response.IsSuccess)
                {
                    await _binRepository.RollbackTransactionAsync();
                    return ApiResponse<string>.Failure("An error occurred while updating the zone status.", statusCode: 500);
                }

                await _binRepository.CommitTransactionAsync();
                return ApiResponse<string>.Success($"Bin and its zone inactivated successfully.", statusCode: 200);
            }

            //if first bin is activating then we need to active zone.
            if (request.Status == EntityStatus.Active && await isFirstBinWillActiveInZone(bin.ZoneId))
            {
                bin.Status = request.Status;
                bin.ModifiedBy = modifiedByUserId;
                bin.ModifiedAt = DateTime.UtcNow;

                await _binRepository.SaveChangesAsync();

                var response = await UpdateZoneStatus(bin.ZoneId, new ZoneStatusUpdateRequest { Status = EntityStatus.Active }, modifiedByUserId);

                if (!response.IsSuccess)
                {
                    await _binRepository.RollbackTransactionAsync();
                    return ApiResponse<string>.Failure("An error occurred while updating the zone status.", statusCode: 500);
                }

                await _binRepository.CommitTransactionAsync();
                return ApiResponse<string>.Success($"Bin and its zone activated successfully.", statusCode: 200);
            }

            bin.Status = request.Status;
            bin.ModifiedBy = modifiedByUserId;
            bin.ModifiedAt = DateTime.UtcNow;

            await _binRepository.SaveChangesAsync();

            await _binRepository.CommitTransactionAsync();
            return ApiResponse<string>.Success($"Bin {bin.Status} successfully.", statusCode: 200);
        }
        catch (Exception)
        {
            await _binRepository.RollbackTransactionAsync();
            return ApiResponse<string>.Failure("An error occurred while updating the bin status.", statusCode: 500);
        }
    }

    private async Task<bool> isLastActiveBinInZone(int zoneId)
    {
        var activeBins = await _binRepository.GetActiveBinByZoneAsync(zoneId);
        return activeBins.Count == 1;
    }

    private async Task<bool> isFirstBinWillActiveInZone(int zoneId)
    {
        var activeBins = await _binRepository.GetActiveBinByZoneAsync(zoneId);
        return activeBins.Count == 0;
    }

    private async Task<ApiResponse<string>> UpdateZoneStatus(int zoneId, ZoneStatusUpdateRequest request, int modifiedByUserId)
    {
        var zone = await _zoneRepository.GetAsync(z => z.Id == zoneId, useNoTracking: false);

        if (zone is null)
            return ApiResponse<string>.Failure("Zone not found.", statusCode: 404);

        var previousStatus = zone.Status;

        zone.Status = request.Status;
        zone.ModifiedBy = modifiedByUserId;
        zone.ModifiedAt = DateTime.UtcNow;

        await _zoneRepository.SaveChangesAsync();
        
        var response = await _zoneService.UpdateWarehouseStatus(zone.WarehouseId, new WarehouseStatusUpdateRequest { Status = request.Status }, modifiedByUserId);
        if (!response.IsSuccess)
        {
            return ApiResponse<string>.Failure("An error occurred while updating the warehouse status.", statusCode: 500);
        }

        return ApiResponse<string>.Success($"Zone {zone.Status} successfully.", statusCode: 200);
    }
}