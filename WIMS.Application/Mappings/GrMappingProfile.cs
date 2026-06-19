using AutoMapper;
using WIMS.Application.DTOs.GoodsReceipts;
using WIMS.Domain.Entity;

namespace WIMS.Application.Mappings;

public class GrMappingProfile : Profile
{
    public GrMappingProfile()
    {
        CreateMap<GoodsReceiptItem, GrItemResponse>()
          .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
          .ForMember(dest => dest.BinName, opt => opt.MapFrom(src => src.Bin.Name))
          .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.Condition.ToString()));

        CreateMap<GoodsReceipt, GrResponse>()
            .ForMember(dest => dest.PoNumber, opt => opt.MapFrom(src => src.PurchaseOrder.PoNumber))
            .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
            .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedByUser.FullName))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
    }
}
