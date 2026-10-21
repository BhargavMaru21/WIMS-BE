using FluentValidation;
using WIMS.Application.DTOs.Warehouse;

namespace WIMS.Application.Validators.WarehouseManagement;


public class ZoneCreateRequestValidator : AbstractValidator<ZoneCreateRequest>
{
    public ZoneCreateRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("A valid WarehouseId is required.");
 
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Zone name is required.")
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z0-9\s\-_]+$").WithMessage("Zone name allows letters, numbers, spaces, hyphens, and underscores (must include a letter).")
            .MinimumLength(2).WithMessage("Zone name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Zone name must not exceed 100 characters.");
 
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid zone status.");
    }
}
 
public class ZoneUpdateRequestValidator : AbstractValidator<ZoneUpdateRequest>
{
    public ZoneUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .Matches(@"^(?=.*[A-Za-z])[A-Za-z0-9\s\-_]+$").WithMessage("Zone name allows letters, numbers, spaces, hyphens, and underscores (must include a letter).")
            .MinimumLength(2).WithMessage("Zone name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Zone name must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
    }
}
 
public class ZoneStatusUpdateRequestValidator : AbstractValidator<ZoneStatusUpdateRequest>
{
    public ZoneStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid zone status.");
    }
}