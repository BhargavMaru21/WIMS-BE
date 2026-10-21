using AutoMapper;
using WIMS.Application.CommonServices;
using WIMS.Application.DTOs.Admin;
using WIMS.Application.DTOs.Profile;
using WIMS.Domain.Entity;

namespace WIMS.Application.Mappings;


public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : null));

        CreateMap<User, UserSummaryResponse>()
            .ForMember(dest => dest.Role,
                opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : null));

        CreateMap<User, UserProfileResponse>()
            .ForMember(dest => dest.Role,
                opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.WarehouseName,
                opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : null))
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => HelperService.ToIST(src.CreatedAt)))
            .ForMember(dest => dest.LastLoginAt,
                opt => opt.MapFrom(src => src.LastLoginAt.HasValue ? HelperService.ToIST(src.LastLoginAt.Value) : (DateTime?)null));
    }
}
