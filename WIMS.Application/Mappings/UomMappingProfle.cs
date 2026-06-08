using AutoMapper;
using WIMS.Application.DTOs.UnitOfMeasure;
using WIMS.Domain.Entity;

namespace WIMS.Application.Mappings;

public class UomMappingProfle : Profile
{
    public UomMappingProfle()
    {
        CreateMap<CreateUnitRequest,UnitsOfMeasure>();
        CreateMap<UnitsOfMeasure,UnitResponse>();
    }
}
