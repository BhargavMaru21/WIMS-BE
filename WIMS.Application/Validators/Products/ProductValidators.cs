using FluentValidation;
using WIMS.Application.DTOs.Products;

namespace WIMS.Application.Validators.Products;

public class ProductCreateRequestValidator : AbstractValidator<ProductCreateRequest>
{
    public ProductCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z0-9\s\-_&().\/]+$").WithMessage("Product name allows letters, numbers, spaces, hyphens, underscores and parentheses (must include a letter).")
            .MinimumLength(2).WithMessage("Product name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");
 
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
 
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("A valid CategoryId is required.");
 
        RuleFor(x => x.UomId)
            .GreaterThan(0).WithMessage("A valid UomId is required.");
 
        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than zero.");
 
        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder level cannot be negative.");
 
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid product status.");
    }
}
 
public class ProductUpdateRequestValidator : AbstractValidator<ProductUpdateRequest>
{
    public ProductUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z0-9\s\-_&().\/]+$").WithMessage("Product name allows letters, numbers, spaces, hyphens, underscores and parentheses (must include a letter).")
            .MinimumLength(2).WithMessage("Product name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
 
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
 
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("A valid CategoryId is required.")
            .When(x => x.CategoryId.HasValue);
 
        RuleFor(x => x.UomId)
            .GreaterThan(0).WithMessage("A valid UomId is required.")
            .When(x => x.UomId.HasValue);
 
        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than zero.")
            .When(x => x.UnitPrice.HasValue);
 
        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder level cannot be negative.")
            .When(x => x.ReorderLevel.HasValue);
    }
}
 
public class ProductStatusUpdateRequestValidator : AbstractValidator<ProductStatusUpdateRequest>
{
    public ProductStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid product status.");
    }
}