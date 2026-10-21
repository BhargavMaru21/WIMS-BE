using FluentValidation;
using WIMS.Application.DTOs.Admin;
using WIMS.Domain.Enums;

namespace WIMS.Application.Validators.Admin;
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Full name can only contain letters and spaces.")
            .MinimumLength(2).WithMessage("Full name must contain at least 2 characters")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");
 
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
             .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$").WithMessage("Invalid email format.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");
 
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"(?=.*[a-z])").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"(?=.*[A-Z])").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"(?=.*\d)").WithMessage("Password must contain at least one number.")
            .Matches(@"(?=.*[\W_])").WithMessage("Password must contain at least one special character.");
 
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role == UserRole.WarehouseManager
                       || role == UserRole.StockKeeper
                       || role == UserRole.Viewer)
            .WithMessage("Only WarehouseManager, StockKeeper, or Viewer can be created. Administrator cannot be assigned.");
 
        RuleFor(x => x.WarehouseId)
            .NotNull()
            .WithMessage("Warehouse is required for WarehouseManager and StockKeeper.")
            .GreaterThan(0)
            .WithMessage("Warehouse ID must be a valid positive number.")
            .When(x => x.Role == UserRole.WarehouseManager || x.Role == UserRole.StockKeeper);

        RuleFor(x => x.WarehouseId)
            .Null()
            .WithMessage("Viewer should not be assigned to a warehouse.")
            .When(x => x.Role == UserRole.Viewer);
    }
}
 