using FluentValidation;
using WIMS.Application.DTOs.Warehouse;

namespace WIMS.Application.Validators.WarehouseManagement;
 
public class BinCreateRequestValidator : AbstractValidator<BinCreateRequest>
{
    public BinCreateRequestValidator()
    {
        RuleFor(x => x.ZoneId)
            .GreaterThan(0).WithMessage("A valid ZoneId is required.");
 
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Bin name is required.")
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z0-9\s\-_]+$").WithMessage("Bin name allows letters, numbers, spaces, hyphens, and underscores (must include a letter).")
            .MinimumLength(2).WithMessage("Bin name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Bin name must not exceed 100 characters.");
 
        RuleFor(x => x.MaxCapacity)
            .GreaterThanOrEqualTo(0).WithMessage("MaxCapacity cannot be negative.");
 
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid bin status.");
    }
}
 
public class BinUpdateRequestValidator : AbstractValidator<BinUpdateRequest>
{
    public BinUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z0-9\s\-_]+$").WithMessage("Bin name allows letters, numbers, spaces, hyphens, and underscores (must include a letter).")
            .MinimumLength(2).WithMessage("Bin name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Bin name must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
 
        RuleFor(x => x.MaxCapacity)
            .GreaterThanOrEqualTo(0).WithMessage("MaxCapacity cannot be negative.")
            .When(x => x.MaxCapacity.HasValue);
    }
}
 
public class BinStatusUpdateRequestValidator : AbstractValidator<BinStatusUpdateRequest>
{
    public BinStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid bin status.");
    }
}
