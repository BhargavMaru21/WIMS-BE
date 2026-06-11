using AutoMapper;
using WIMS.Application.DTOs.ProductCategory;
using WIMS.Application.DTOs.Products;
using WIMS.Application.DTOs.UnitOfMeasure;
using WIMS.Domain.Entity;

namespace WIMS.Application.Mappings;

public class ProductManagementMappingProfile : Profile
{
    public ProductManagementMappingProfile()
    {

        //UOM
        CreateMap<CreateUnitRequest, UnitsOfMeasure>();
        CreateMap<UnitsOfMeasure, UnitResponse>();

        //Productategory
        CreateMap<ProductCategoryCreateRequest, ProductCategory>();
        CreateMap<ProductCategory, ProductCategoryResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        CreateMap<ProductCategory, ProductCategoryDropdownResponse>();


        //Product
        CreateMap<ProductCreateRequest, Product>()
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => "Temp"));

        CreateMap<Product, ProductResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.UomName, opt => opt.MapFrom(src => src.Uom.Name))
            .ForMember(dest => dest.UomAbbreviation, opt => opt.MapFrom(src => src.Uom.Abbreviation));
    }
}
