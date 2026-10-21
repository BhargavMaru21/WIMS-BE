using AutoMapper;
using WIMS.Application.DTOs.Warehouse;
using WIMS.Domain.Entity;

namespace WIMS.Application.Mappings;

public class WarehouseMappingProfile : Profile
{
    public WarehouseMappingProfile()
    {
        //warehouse
        CreateMap<Warehouse, WarehouseResponse>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<WarehouseCreateRequest, Warehouse>()
            .ForMember(dest => dest.Code,
                opt => opt.MapFrom(src => "Temp"));


        //zone  
 
        CreateMap<ZoneCreateRequest, Zone>()
            .ForMember(dest => dest.Code,
                opt => opt.MapFrom(src => "Temp"));

        CreateMap<Zone, ZoneResponse>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse.Name));
 
        CreateMap<Zone, ZoneDropdownResponse>();


        //Bins

        CreateMap<BinCreateRequest, Bin>()
            .ForMember(dest => dest.Code,
                opt => opt.MapFrom(src => "Temp"));


        CreateMap<Bin, BinResponse>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ZoneName,
                opt => opt.MapFrom(src => src.Zone.Name ))
            .ForMember(dest => dest.WarehouseId,
                opt => opt.MapFrom(src =>  src.Zone.WarehouseId))
            .ForMember(dest => dest.WarehouseName,
                opt => opt.MapFrom(src =>
                   src.Zone.Warehouse.Name));
                        
        CreateMap<Bin, BinDropdownResponse>()
            .ForMember(dest => dest.WarehouseId,
                opt => opt.MapFrom(src => src.Zone.WarehouseId));
 
    }
}
