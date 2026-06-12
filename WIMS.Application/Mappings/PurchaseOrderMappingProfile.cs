using AutoMapper;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Domain.Entity;

namespace WIMS.Application.Mappings;

public class PurchaseOrderMappingProfile : Profile
{
    public PurchaseOrderMappingProfile(){
        CreateMap<PurchaseOrderCreateRequest , PurchaseOrder>()
            .ForMember(dest => dest.PoNumber , opt => opt.MapFrom(src => "TEMP"));

        CreateMap<PurchaseOrder , PurchaseOrderResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
