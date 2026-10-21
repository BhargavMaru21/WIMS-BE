using FluentValidation;
using WIMS.Application.DTOs.ProductCategory;

namespace WIMS.Application.Validators.ProductCategory;


public class ProductCategoryCreateRequestValidator : AbstractValidator<ProductCategoryCreateRequest>
{
    public ProductCategoryCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z\s\-_()]+$").WithMessage("Category name allows letters, spaces, hyphens, underscores and parentheses (must include a letter).")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");
 
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
 
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid category status.");
    }
}   
 
public class ProductCategoryUpdateRequestValidator : AbstractValidator<ProductCategoryUpdateRequest>
{
    public ProductCategoryUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z\s\-_()]+$").WithMessage("Category name allows letters, spaces, hyphens, underscores and parentheses (must include a letter).")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
 
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
 
public class ProductCategoryStatusUpdateRequestValidator : AbstractValidator<ProductCategoryStatusUpdateRequest>
{
    public ProductCategoryStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid category status.");
    }
}