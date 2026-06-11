using AutoMapper;
using ClosedXML.Excel;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Products;
using WIMS.Application.DTOs.UnitOfMeasure;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.UnitOfMeasure;
using WIMS.Domain.Entity;

namespace WIMS.Application.Service.UnitOfMeasure;

public class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly IMapper _mapper;

    public UnitOfMeasureService(IUnitOfMeasureRepository unitOfMeasureRepository, IInputNormalizer inputNormalizer, IMapper mapper)
    {
        _unitOfMeasureRepository = unitOfMeasureRepository;
        _inputNormalizer = inputNormalizer;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UnitResponse>> CreateUnit(CreateUnitRequest request, int createdByUserId)
    {
        request = _inputNormalizer.NormalizeObject(request);

        if (await _unitOfMeasureRepository.ExistsAsync(x => x.Name.ToLower() == request.Name.ToLower()))
        {
            return ApiResponse<UnitResponse>.Failure("Unit name already exists.", statusCode: 400);
        }

        if (await _unitOfMeasureRepository.ExistsAsync(x => x.Abbreviation.ToLower() == request.Abbreviation.ToLower()))
        {
            return ApiResponse<UnitResponse>.Failure("Unit Abbreviation already exists.", statusCode: 400);
        }

        var UnitEntity = _mapper.Map<UnitsOfMeasure>(request);
        UnitEntity.CreatedBy = createdByUserId;

        var createdUnit = await _unitOfMeasureRepository.CreateAsync(UnitEntity);

        var result = _mapper.Map<UnitResponse>(createdUnit);

        return ApiResponse<UnitResponse>.Success(result, "Unit created successfully.", statusCode: 201);
    }

    public async Task<ApiResponse<List<UnitResponse>>> GetUnitsDropdown()
    {
        var allUnits = await _unitOfMeasureRepository.GetAllAsync(orderBy: q => q.OrderBy(u => u.Name));

        var result = _mapper.Map<List<UnitResponse>>(allUnits);

        return ApiResponse<List<UnitResponse>>.Success(result, statusCode: 200);
    }

    public async Task<ApiResponse<UnitResponse>> UpdateUnit(int id, UpdateUnitRequest request)
    {
        request = _inputNormalizer.NormalizeObject(request);

        var uom = await _unitOfMeasureRepository.GetAsync(x => x.Id == id, useNoTracking: false);

        if (uom is null)
            return ApiResponse<UnitResponse>.Failure("Unit of measure not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(request.Name) &&
            await _unitOfMeasureRepository.ExistsAsync(x => x.Name.ToLower() == request.Name.ToLower() && x.Id != id))
            return ApiResponse<UnitResponse>.Failure("A unit of measure with this name already exists.", statusCode: 400);

        if (!string.IsNullOrWhiteSpace(request.Abbreviation) &&
            await _unitOfMeasureRepository.ExistsAsync(x => x.Abbreviation.ToLower() == request.Abbreviation.ToLower() && x.Id != id))
            return ApiResponse<UnitResponse>.Failure("A unit of measure with this abbreviation already exists.", statusCode: 400);

        uom.Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : uom.Name;
        uom.Abbreviation = !string.IsNullOrWhiteSpace(request.Abbreviation) ? request.Abbreviation : uom.Abbreviation;

        await _unitOfMeasureRepository.SaveChangesAsync();
        var response = _mapper.Map<UnitResponse>(uom);

        return ApiResponse<UnitResponse>.Success(response, "Unit of measure updated successfully.", statusCode: 200);
    }

    public async Task<ApiResponse<string>> DeleteUnit(int id)
    {
        var uom = await _unitOfMeasureRepository.GetAsync(x => x.Id == id,useNoTracking : false);

        if(uom is null)
        {
            return ApiResponse<string>.Failure("Unit not Found",statusCode:404);
        }

        if(await _unitOfMeasureRepository.IsAssignedToProductAsync(uom.Id))
        {
            return ApiResponse<string>.Failure("Unit is Assigned to Product. It can not be delete",statusCode:400);
        }

        if(await _unitOfMeasureRepository.DeleteAsync(uom))
            return ApiResponse<string>.Success("Unit Deleted Successfully",statusCode:200);   

        return ApiResponse<string>.Failure("Error occur while Deleting Unit",statusCode:500);
    }

}
