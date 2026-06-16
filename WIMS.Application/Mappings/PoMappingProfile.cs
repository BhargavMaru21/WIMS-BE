using AutoMapper;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Domain.Entity;

namespace WIMS.Application.Mappings;

public class PoMappingProfile : Profile
{
    public PoMappingProfile()
    {
        CreateMap<PurchaseOrderItem, PoItemResponse>()
            .ForMember(dest => dest.ProductName, opt => opt.Ignore());
 
        CreateMap<PurchaseOrder, PoResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(dest => dest.SubmittedByName, opt => opt.MapFrom(src => src.SubmittedBy.HasValue ? src.SubmittedByUser!.FullName : null))
            .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy.HasValue ? src.ApprovedByUser!.FullName : null))
            .ForMember(dest => dest.RejectedByName, opt => opt.MapFrom(src => src.RejectedBy.HasValue ? src.RejectedByUser!.FullName : null))
            .ForMember(dest => dest.CancelledByName, opt => opt.MapFrom(src => src.CancelledBy.HasValue ? src.CancelledByUser!.FullName : null))
            .ForMember(dest => dest.CanApprove, opt => opt.Ignore())
            .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
 
        CreateMap<PoCreateRequest, PurchaseOrder>()
            .ForMember(dest => dest.PoNumber, opt => opt.MapFrom(src => "TEMP"));
    }
}
