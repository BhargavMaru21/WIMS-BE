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

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IZoneRepository _zoneRepository;
    private readonly IBinRepository _binRepository;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly ICodeGeneratorService _codeGeneratorService;
    private readonly ICurrentUserService _currentUser;

    public WarehouseService(
        IWarehouseRepository warehouseRepository,
        IMapper mapper,
        IInputNormalizer inputNormalizer,
        ICodeGeneratorService codeGeneratorService,
        IZoneRepository zoneRepository,
        IBinRepository binRepository,
        ICurrentUserService currentUser
        )
    {
        _warehouseRepository = warehouseRepository;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
        _codeGeneratorService = codeGeneratorService;
        _zoneRepository = zoneRepository;
        _binRepository = binRepository;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<WarehouseResponse>> CreateWarehouse(WarehouseCreateRequest request)
    {
        int createdByUserId = _currentUser.GetUserId();
        request = _inputNormalizer.NormalizeObject(request);

        if (await _warehouseRepository.ExistsAsync(x => x.Name.ToLower() == request.Name.ToLower()))
        {
            return ApiResponse<WarehouseResponse>.Failure("Warehouse name already exists.", statusCode: 400);
        }

        var warehouseEntity = _mapper.Map<Warehouse>(request);
        warehouseEntity.CreatedBy = createdByUserId;
        var createdWarehouse = await _warehouseRepository.CreateAsync(warehouseEntity);

        string generateCode = _codeGeneratorService.GenerateCode("warehouse", createdWarehouse.Id);
        createdWarehouse.Code = generateCode;

        await _warehouseRepository.SaveChangesAsync();
        var response = _mapper.Map<WarehouseResponse>(createdWarehouse);

        return ApiResponse<WarehouseResponse>.Success(response, "Warehouse created successfully.", statusCode: 201);

    }

    public async Task<ApiResponse<WarehouseResponse>> GetWarehouseById(int id)
    {
        if (id <= 0)
            return ApiResponse<WarehouseResponse>.Failure("Invalid Id", statusCode: 400);

        var warehouse = await _warehouseRepository.GetAsync(w => w.Id == id);

        if (warehouse is null)
            return ApiResponse<WarehouseResponse>.Failure("Warehouse not found.", statusCode: 404);

        var response = _mapper.Map<WarehouseResponse>(warehouse);
        return ApiResponse<WarehouseResponse>.Success(response, statusCode: 200);
    }

    public async Task<ApiResponse<PagedResult<WarehouseResponse>>> GetWarehouses(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);

        var pagedWarehouses = await _warehouseRepository.GetPaginatedAsync(
            qp,
            searchableColumns: ["Name", "City", "Code"]
            );

        var result = new PagedResult<WarehouseResponse>
        {
            Items = _mapper.Map<List<WarehouseResponse>>(pagedWarehouses.Items),
            TotalCount = pagedWarehouses.TotalCount,
            PageSize = pagedWarehouses.PageSize,
            PageNumber = pagedWarehouses.PageNumber
        };
        return ApiResponse<PagedResult<WarehouseResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<WarehouseResponse>> UpdateWarehouse(int id, WarehouseUpdateRequest request)
    {
        if (id <= 0)
            return ApiResponse<WarehouseResponse>.Failure("Invalid Id", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();
        request = _inputNormalizer.NormalizeObject(request);

        var warehouse = await _warehouseRepository.GetAsync(w => w.Id == id, useNoTracking: false);

        if (warehouse is null)
            return ApiResponse<WarehouseResponse>.Failure("Warehouse not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(request.Name) && await _warehouseRepository.ExistsAsync(w => w.Name.ToLower() == request.Name.ToLower() && w.Id != warehouse.Id))
        {
            return ApiResponse<WarehouseResponse>.Failure("Warehouse name already exists.", statusCode: 400);
        }

        warehouse.Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : warehouse.Name;
        warehouse.Address = !string.IsNullOrWhiteSpace(request.Address) ? request.Address : warehouse.Address;
        warehouse.City = !string.IsNullOrWhiteSpace(request.City) ? request.City : warehouse.City;
        warehouse.ContactPerson = !string.IsNullOrWhiteSpace(request.ContactPerson) ? request.ContactPerson : warehouse.ContactPerson;
        warehouse.ContactPhone = !string.IsNullOrWhiteSpace(request.ContactPhone) ? request.ContactPhone : warehouse.ContactPhone;
        warehouse.ModifiedBy = modifiedByUserId;
        warehouse.ModifiedAt = DateTime.UtcNow;

        await _warehouseRepository.SaveChangesAsync();
        var response = _mapper.Map<WarehouseResponse>(warehouse);

        return ApiResponse<WarehouseResponse>.Success(response, "Warehouse updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> UpdateWarehouseStatus(int id, WarehouseStatusUpdateRequest request)
    {
        if (id <= 0)
            return ApiResponse<string>.Failure("Invalid Id", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();
        var warehouse = await _warehouseRepository.GetAsync(w => w.Id == id, useNoTracking: false, includes: q => q.Include(w => w.Zones));

        if (warehouse is null)
            return ApiResponse<string>.Failure("Warehouse not found.", statusCode: 404);

        if (warehouse.Status == request.Status)
        {
            return ApiResponse<string>.Failure($"Warehouse is already {warehouse.Status}.", statusCode: 400);
        }

        //any zone is active then we can not inactive warehouse
        if (request.Status == EntityStatus.Inactive && warehouse.Zones.Any(z => z.Status == EntityStatus.Active) && await _warehouseRepository.HasStockAsync(warehouse.Id))
        {
            return ApiResponse<string>.Failure("Cannot inactivate warehouse with active zones and Stock. Please inactivate all zones first.", statusCode: 400);
        }

        //if all zones are inactive then we can not active warehouse
        if (request.Status == EntityStatus.Active && warehouse.Zones.Count > 0 && warehouse.Zones.All(z => z.Status == EntityStatus.Inactive) && await _warehouseRepository.HasStockAsync(warehouse.Id))
        {
            return ApiResponse<string>.Failure(
                "Cannot activate warehouse with all zones inactive. Please activate at least one zone first.",
                statusCode: 400);
        }

        warehouse.Status = request.Status;
        warehouse.ModifiedBy = modifiedByUserId;
        warehouse.ModifiedAt = DateTime.UtcNow;

        await _warehouseRepository.SaveChangesAsync();

        return ApiResponse<string>.Success($"Warehouse {warehouse.Status} successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> DeleteWarehouse(int id)
    {
        if (id <= 0)
            return ApiResponse<string>.Failure("Invalid Id", statusCode: 400);

        int deletedBy = _currentUser.GetUserId();
        var warehouse = await _warehouseRepository.GetAsync(w => w.Id == id, useNoTracking: false, includes: q => q.Include(z => z.Zones).ThenInclude(b => b.Bins));

        if (warehouse is null)
            return ApiResponse<string>.Failure("Warehouse not found.", statusCode: 404);

        if (await _warehouseRepository.HasStockAsync(id))
            return ApiResponse<string>.Failure("Warehouse Has Stock.Please Remove Stock For Delete");

        await _warehouseRepository.BeginTransactionAsync();

        try
        {
            foreach (var zone in warehouse.Zones.ToList())
            {
                foreach (var bin in zone.Bins.ToList())
                {
                    await _binRepository.SoftDeleteAsync(bin, deletedBy);
                }
                await _zoneRepository.SoftDeleteAsync(zone, deletedBy);
            }

            await _warehouseRepository.SoftDeleteAsync(warehouse, deletedBy);

            await _warehouseRepository.CommitTransactionAsync();
            return ApiResponse<string>.Success("Warehouse and it's related Zone & Bins are Deleted ");

        }
        catch (Exception)
        {
            await _warehouseRepository.RollbackTransactionAsync();
            return ApiResponse<string>.Failure("an error occure while deleting warehouse");
        }


    }




}
